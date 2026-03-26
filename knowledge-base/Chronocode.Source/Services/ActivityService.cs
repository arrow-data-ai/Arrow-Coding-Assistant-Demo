using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for managing activities and time tracking with role-based access control
/// </summary>
public class ActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthorizationService _authService;

    public ActivityService(ApplicationDbContext context, AuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Gets activities for a specific user and date range - only from projects they have access to
    /// </summary>
    public async Task<List<Activity>> GetUserActivitiesAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var accessibleProjectIds = await _authService.GetAccessibleProjectIdsAsync(userId);
        
        return await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate >= startDate && 
                       a.ActivityDate <= endDate && 
                       a.IsActive &&
                       accessibleProjectIds.Contains(a.Task.ProjectId))
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.Name)
            .AsNoTracking() // Ensure we get fresh data from the database
            .ToListAsync();
    }

    /// <summary>
    /// Gets activities for a specific task - only if user has access to the project
    /// </summary>
    public async Task<List<Activity>> GetTaskActivitiesAsync(int taskId, string userId)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null || !await _authService.CanAccessProjectAsync(userId, task.ProjectId))
            return new List<Activity>();

        return await _context.Activities
            .Where(a => a.TaskId == taskId && a.IsActive)
            .Include(a => a.User)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific activity by ID
    /// </summary>
    public async Task<Activity?> GetActivityByIdAsync(int activityId)
    {
        return await _context.Activities
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.User)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.IsActive);
    }

    /// <summary>
    /// Creates a new activity with charge code allocations
    /// </summary>
    public async Task<Activity> CreateActivityAsync(Activity activity, List<(int ChargeCodeId, decimal Hours)> chargeCodeAllocations)
    {
        // Validate that the task is not completed
        var task = await _context.ProjectTasks.FindAsync(activity.TaskId);
        if (task?.IsComplete == true)
        {
            throw new InvalidOperationException("Cannot add activities to a completed task.");
        }

        // Validate that all charge codes are valid for the activity date
        await ValidateChargeCodesAsync(chargeCodeAllocations.Select(cc => cc.ChargeCodeId).ToList(), activity.ActivityDate);

        activity.CreatedDate = DateTime.UtcNow;
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        // Add charge code allocations
        foreach (var allocation in chargeCodeAllocations)
        {
            _context.ActivityChargeCodes.Add(new ActivityChargeCode
            {
                ActivityId = activity.Id,
                ChargeCodeId = allocation.ChargeCodeId,
                AllocatedHours = allocation.Hours,
                CreatedDate = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        return activity;
    }

    /// <summary>
    /// Updates an existing activity - validates user ownership
    /// </summary>
    public async Task<Activity> UpdateActivityAsync(Activity activity, List<(int ChargeCodeId, decimal Hours)> chargeCodeAllocations, string requestingUserId)
    {
        var existingActivity = await _context.Activities.FindAsync(activity.Id);
        if (existingActivity == null)
        {
            throw new InvalidOperationException($"Activity with ID {activity.Id} not found.");
        }

        // SECURITY: Validate user ownership - users can only update their own activities
        if (existingActivity.UserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this activity.");
        }

        // Validate that the task is not completed (if the task is being changed)
        if (existingActivity.TaskId != activity.TaskId)
        {
            var newTask = await _context.ProjectTasks.FindAsync(activity.TaskId);
            if (newTask?.IsComplete == true)
            {
                throw new InvalidOperationException("Cannot move activities to a completed task.");
            }
        }

        // Validate that all charge codes are valid for the activity date
        await ValidateChargeCodesAsync(chargeCodeAllocations.Select(cc => cc.ChargeCodeId).ToList(), activity.ActivityDate);

        // Update properties manually to avoid tracking conflicts
        existingActivity.Name = activity.Name;
        existingActivity.Description = activity.Description;
        existingActivity.TaskId = activity.TaskId;
        // SECURITY: Do NOT allow UserId to be changed
        existingActivity.DurationHours = activity.DurationHours;
        existingActivity.ActivityDate = activity.ActivityDate;
        existingActivity.ModifiedDate = DateTime.UtcNow;

        // Remove existing charge code allocations
        var existingAllocations = await _context.ActivityChargeCodes
            .Where(acc => acc.ActivityId == activity.Id)
            .ToListAsync();
        _context.ActivityChargeCodes.RemoveRange(existingAllocations);

        // Add new charge code allocations
        foreach (var allocation in chargeCodeAllocations)
        {
            _context.ActivityChargeCodes.Add(new ActivityChargeCode
            {
                ActivityId = activity.Id,
                ChargeCodeId = allocation.ChargeCodeId,
                AllocatedHours = allocation.Hours,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return existingActivity;
    }

    /// <summary>
    /// Deletes an activity (soft delete)
    /// </summary>
    public async Task DeleteActivityAsync(int activityId)
    {
        var activity = await _context.Activities.FindAsync(activityId);
        if (activity != null)
        {
            activity.IsActive = false;
            activity.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Distributes activity hours evenly across selected charge codes (Peanut Butter Spread)
    /// </summary>
    public List<(int ChargeCodeId, decimal Hours)> DistributeHoursEvenly(decimal totalHours, List<int> chargeCodeIds)
    {
        if (chargeCodeIds.Count == 0)
            return new List<(int, decimal)>();

        var hoursPerCode = Math.Round(totalHours / chargeCodeIds.Count, 2);
        var result = new List<(int, decimal)>();

        // Distribute hours evenly
        for (int i = 0; i < chargeCodeIds.Count; i++)
        {
            if (i == chargeCodeIds.Count - 1)
            {
                // Last charge code gets any remaining hours due to rounding
                var remainingHours = totalHours - (result.Sum(r => r.Item2));
                result.Add((chargeCodeIds[i], remainingHours));
            }
            else
            {
                result.Add((chargeCodeIds[i], hoursPerCode));
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that all charge codes are valid for the specified date
    /// </summary>
    private async Task ValidateChargeCodesAsync(List<int> chargeCodeIds, DateTime activityDate)
    {
        var invalidChargeCodes = new List<string>();
        
        foreach (var chargeCodeId in chargeCodeIds)
        {
            var chargeCode = await _context.ChargeCodes
                .FirstOrDefaultAsync(cc => cc.Id == chargeCodeId);
                
            if (chargeCode == null)
            {
                invalidChargeCodes.Add($"Charge code with ID {chargeCodeId} not found");
            }
            else if (!chargeCode.IsActive)
            {
                invalidChargeCodes.Add($"Charge code '{chargeCode.Code}' is inactive");
            }
            else if (chargeCode.ValidFromDate > activityDate || chargeCode.ValidToDate < activityDate)
            {
                invalidChargeCodes.Add($"Charge code '{chargeCode.Code}' is not valid for date {activityDate:yyyy-MM-dd} (valid from {chargeCode.ValidFromDate:yyyy-MM-dd} to {chargeCode.ValidToDate:yyyy-MM-dd})");
            }
        }
        
        if (invalidChargeCodes.Any())
        {
            throw new InvalidOperationException($"Invalid charge codes detected: {string.Join(", ", invalidChargeCodes)}");
        }
    }
}
