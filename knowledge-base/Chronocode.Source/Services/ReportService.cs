using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for generating activity reports
/// </summary>
public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates a daily activity report for a user
    /// </summary>
    public async Task<ActivityReport> GenerateDailyReportAsync(string userId, DateTime date)
    {
        var activities = await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate.Date == date.Date && 
                       a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return new ActivityReport
        {
            UserId = userId,
            StartDate = date,
            EndDate = date,
            Activities = activities,
            TotalHours = activities.Sum(a => a.DurationHours),
            GeneratedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a weekly activity report for a user
    /// </summary>
    public async Task<ActivityReport> GenerateWeeklyReportAsync(string userId, DateTime weekStartDate)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        
        var activities = await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate >= weekStartDate && 
                       a.ActivityDate <= weekEndDate && 
                       a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.Name)
            .ToListAsync();

        return new ActivityReport
        {
            UserId = userId,
            StartDate = weekStartDate,
            EndDate = weekEndDate,
            Activities = activities,
            TotalHours = activities.Sum(a => a.DurationHours),
            GeneratedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a custom date range activity report for a user
    /// </summary>
    public async Task<ActivityReport> GenerateCustomReportAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var activities = await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate >= startDate && 
                       a.ActivityDate <= endDate && 
                       a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.Name)
            .ToListAsync();

        return new ActivityReport
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            Activities = activities,
            TotalHours = activities.Sum(a => a.DurationHours),
            GeneratedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets activity summary by charge code for a date range
    /// </summary>
    public async Task<List<ChargeCodeSummary>> GetChargeCodeSummaryAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var results = await _context.ActivityChargeCodes
            .Where(acc => acc.Activity.UserId == userId &&
                         acc.Activity.ActivityDate >= startDate &&
                         acc.Activity.ActivityDate <= endDate &&
                         acc.Activity.IsActive)
            .GroupBy(acc => acc.ChargeCode.Code) // Group by Charge Code only
            .Select(g => new ChargeCodeSummary
            {
                ChargeCode = g.Key,
                ChargeCodeName = g.First().ChargeCode.Name, // Use the name from the first entry
                TotalHours = g.Sum(acc => acc.AllocatedHours),
                ActivityCount = g.Count()
            })
            .OrderBy(s => s.ChargeCode)
            .ToListAsync();

        // Adjust for F1 display compatibility
        return AdjustChargeCodeSummaryForDisplay(results);
    }

    /// <summary>
    /// Adjusts charge code summary values to ensure F1 formatted values add up to the correct total
    /// </summary>
    public static List<ChargeCodeSummary> AdjustChargeCodeSummaryForDisplay(List<ChargeCodeSummary> summaries)
    {
        if (!summaries.Any()) return summaries;

        var actualTotal = summaries.Sum(s => s.TotalHours);
        
        // Find the charge code with the lowest value (alphabetically last if tied)
        var lowestSummary = summaries.OrderBy(s => s.TotalHours).ThenByDescending(s => s.ChargeCode).First();
        
        // Round all other values to F1, keeping track of the total
        var othersRoundedTotal = 0m;
        foreach (var summary in summaries)
        {
            if (summary != lowestSummary)
            {
                summary.TotalHours = Math.Round(summary.TotalHours, 1, MidpointRounding.AwayFromZero);
                othersRoundedTotal += summary.TotalHours;
            }
        }
        
        // The lowest charge code gets the remainder to ensure the total matches exactly
        lowestSummary.TotalHours = actualTotal - othersRoundedTotal;

        return summaries;
    }

    /// <summary>
    /// Gets percentage of work done per project for a user in a date range
    /// </summary>
    public async Task<List<ProjectWorkPercentage>> GetProjectWorkPercentagesAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var activities = await _context.Activities
            .Where(a => a.UserId == userId &&
                        a.ActivityDate >= startDate &&
                        a.ActivityDate <= endDate &&
                        a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .ToListAsync();

        var totalHours = activities.Sum(a => a.DurationHours);
        var projectGroups = activities
            .GroupBy(a => a.Task.Project)
            .Select(g => new ProjectWorkPercentage
            {
                ProjectId = g.Key.Id,
                ProjectName = g.Key.Name,
                TotalHours = g.Sum(a => a.DurationHours),
                Percentage = totalHours > 0 ? Math.Round((double)g.Sum(a => a.DurationHours) / (double)totalHours * 100, 2) : 0
            })
            .OrderByDescending(p => p.Percentage)
            .ToList();

        return projectGroups;
    }

    /// <summary>
    /// Generates a task-based activity report for a specific task
    /// </summary>
    public async Task<TaskActivityReport> GenerateTaskReportAsync(string userId, int taskId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Activities
            .Where(a => a.UserId == userId && 
                       a.TaskId == taskId && 
                       a.IsActive);

        if (startDate.HasValue)
            query = query.Where(a => a.ActivityDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.ActivityDate <= endDate.Value);

        var activities = await query
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .Include(a => a.ActivityChargeCodes)
                .ThenInclude(acc => acc.ChargeCode)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var task = await _context.ProjectTasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        return new TaskActivityReport
        {
            UserId = userId,
            TaskId = taskId,
            TaskName = task?.Name ?? "Unknown Task",
            ProjectName = task?.Project?.Name ?? "Unknown Project",
            StartDate = startDate ?? (activities.Count == 0 ? DateTime.Today : activities.Min(a => a.ActivityDate)),
            EndDate = endDate ?? (activities.Count == 0 ? DateTime.Today : activities.Max(a => a.ActivityDate)),
            Activities = activities,
            TotalHours = activities.Sum(a => a.DurationHours),
            GeneratedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets all tasks for a user that have activities
    /// </summary>
    public async Task<List<TaskSummary>> GetUserTasksWithActivitiesAsync(string userId)
    {
        var tasksWithActivities = await _context.Activities
            .Where(a => a.UserId == userId && a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .ToListAsync();

        var taskSummaries = tasksWithActivities
            .GroupBy(a => a.Task)
            .Select(g => new TaskSummary
            {
                TaskId = g.Key.Id,
                TaskName = g.Key.Name,
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.Project.Name,
                ActivityCount = g.Count(),
                TotalHours = g.Sum(a => a.DurationHours),
                LastActivityDate = g.Max(a => a.ActivityDate)
            })
            .OrderBy(t => t.ProjectName)
            .ThenBy(t => t.TaskName)
            .ToList();

        return taskSummaries;
    }

    /// <summary>
    /// Gets all projects for a user that have activities
    /// </summary>
    public async Task<List<ProjectSummary>> GetUserProjectsWithActivitiesAsync(string userId)
    {
        var activitiesWithProjects = await _context.Activities
            .Where(a => a.UserId == userId && a.IsActive)
            .Include(a => a.Task)
                .ThenInclude(t => t.Project)
            .ToListAsync();

        var projectSummaries = activitiesWithProjects
            .GroupBy(a => a.Task.Project)
            .Select(g => new ProjectSummary
            {
                ProjectId = g.Key.Id,
                ProjectName = g.Key.Name,
                TaskCount = g.Select(a => a.Task).Distinct().Count(),
                ActivityCount = g.Count(),
                TotalHours = g.Sum(a => a.DurationHours),
                LastActivityDate = g.Max(a => a.ActivityDate)
            })
            .OrderBy(p => p.ProjectName)
            .ToList();

        return projectSummaries;
    }

    /// <summary>
    /// Gets charge code summary for a specific task
    /// </summary>
    public async Task<List<ChargeCodeSummary>> GetTaskChargeCodeSummaryAsync(string userId, int taskId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.ActivityChargeCodes
            .Where(acc => acc.Activity.UserId == userId &&
                         acc.Activity.TaskId == taskId &&
                         acc.Activity.IsActive);

        if (startDate.HasValue)
            query = query.Where(acc => acc.Activity.ActivityDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(acc => acc.Activity.ActivityDate <= endDate.Value);

        var results = await query
            .GroupBy(acc => acc.ChargeCode.Code)
            .Select(g => new ChargeCodeSummary
            {
                ChargeCode = g.Key,
                ChargeCodeName = g.First().ChargeCode.Name,
                TotalHours = g.Sum(acc => acc.AllocatedHours),
                ActivityCount = g.Count()
            })
            .OrderBy(s => s.ChargeCode)
            .ToListAsync();

        // Adjust for F1 display compatibility
        return AdjustChargeCodeSummaryForDisplay(results);
    }

    /// <summary>
    /// Gets all charge codes from projects where user has activities, including unused charge codes
    /// </summary>
    public async Task<List<ChargeCodeOption>> GetUserChargeCodesWithActivitiesAsync(string userId)
    {
        // Get all projects where the user has activities
        var userProjectIds = await _context.Activities
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => a.Task.ProjectId)
            .Distinct()
            .ToListAsync();

        // Get all charge codes from those projects
        var chargeCodes = await _context.ChargeCodes
            .Where(cc => userProjectIds.Contains(cc.ProjectId))
            .Include(cc => cc.Project)
            .OrderBy(cc => cc.Code)
            .ToListAsync();

        // Get charge codes that have been used in activities
        var usedChargeCodeIds = await _context.ActivityChargeCodes
            .Where(acc => acc.Activity.UserId == userId && acc.Activity.IsActive)
            .Select(acc => acc.ChargeCodeId)
            .Distinct()
            .ToListAsync();

        return chargeCodes.Select(cc => new ChargeCodeOption
        {
            ChargeCodeId = cc.Id,
            Code = cc.Code,
            Name = cc.Name,
            ProjectId = cc.ProjectId,
            ProjectName = cc.Project.Name,
            IsActive = cc.IsActive,
            IsExpired = cc.ValidToDate < DateTime.Today,
            ValidFromDate = cc.ValidFromDate,
            ValidToDate = cc.ValidToDate,
            HasActivities = usedChargeCodeIds.Contains(cc.Id)
        }).ToList();
    }

    /// <summary>
    /// Generates a charge code report showing all activities associated with selected charge codes
    /// </summary>
    public async Task<ChargeCodeReport> GenerateChargeCodeReportAsync(string userId, List<int> chargeCodeIds, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.ActivityChargeCodes
            .Where(acc => acc.Activity.UserId == userId &&
                         chargeCodeIds.Contains(acc.ChargeCodeId) &&
                         acc.Activity.IsActive);

        if (startDate.HasValue)
            query = query.Where(acc => acc.Activity.ActivityDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(acc => acc.Activity.ActivityDate <= endDate.Value);

        var activityChargeCodeData = await query
            .Include(acc => acc.Activity)
                .ThenInclude(a => a.Task)
                    .ThenInclude(t => t.Project)
            .Include(acc => acc.ChargeCode)
            .OrderBy(acc => acc.Activity.ActivityDate)
            .ThenBy(acc => acc.Activity.Name)
            .ToListAsync();

        // Get unique activities
        var activities = activityChargeCodeData
            .Select(acc => acc.Activity)
            .DistinctBy(a => a.Id)
            .ToList();

        // Get charge code summaries
        var chargeCodeSummaries = activityChargeCodeData
            .GroupBy(acc => acc.ChargeCode)
            .Select(g => new ChargeCodeSummary
            {
                ChargeCode = g.Key.Code,
                ChargeCodeName = g.Key.Name,
                ProjectId = g.Key.ProjectId,
                TotalHours = g.Sum(acc => acc.AllocatedHours),
                ActivityCount = g.Select(acc => acc.ActivityId).Distinct().Count()
            })
            .OrderBy(s => s.ChargeCode)
            .ToList();

        // Get selected charge codes info
        var selectedChargeCodes = await _context.ChargeCodes
            .Include(cc => cc.Project)
            .Where(cc => chargeCodeIds.Contains(cc.Id))
            .OrderBy(cc => cc.Code)
            .ToListAsync();

        return new ChargeCodeReport
        {
            UserId = userId,
            StartDate = startDate ?? (activities.Count == 0 ? DateTime.Today : activities.Min(a => a.ActivityDate)),
            EndDate = endDate ?? (activities.Count == 0 ? DateTime.Today : activities.Max(a => a.ActivityDate)),
            SelectedChargeCodes = selectedChargeCodes.Select(cc => new ChargeCodeOption
            {
                ChargeCodeId = cc.Id,
                Code = cc.Code,
                Name = cc.Name,
                ProjectId = cc.ProjectId,
                ProjectName = cc.Project.Name,
                IsActive = cc.IsActive,
                IsExpired = cc.ValidToDate < DateTime.Today,
                ValidFromDate = cc.ValidFromDate,
                ValidToDate = cc.ValidToDate
            }).ToList(),
            Activities = activities,
            ChargeCodeSummaries = AdjustChargeCodeSummaryForDisplay(chargeCodeSummaries),
            TotalHours = chargeCodeSummaries.Sum(s => s.TotalHours),
            GeneratedDate = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents an activity report
/// </summary>
public class ActivityReport
{
    public string UserId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Activity> Activities { get; set; } = new List<Activity>();
    public decimal TotalHours { get; set; }
    public DateTime GeneratedDate { get; set; }
}

/// <summary>
/// Represents a charge code summary
/// </summary>
public class ChargeCodeSummary
{
    public string ChargeCode { get; set; } = string.Empty;
    public string ChargeCodeName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public decimal TotalHours { get; set; }
    public int ActivityCount { get; set; }
}

/// <summary>
/// Represents percentage of work done per project
/// </summary>
public class ProjectWorkPercentage
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Represents a task-based activity report
/// </summary>
public class TaskActivityReport
{
    public string UserId { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Activity> Activities { get; set; } = new List<Activity>();
    public decimal TotalHours { get; set; }
    public DateTime GeneratedDate { get; set; }
}

/// <summary>
/// Represents a summary of a task with activity information
/// </summary>
public class TaskSummary
{
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public decimal TotalHours { get; set; }
    public DateTime LastActivityDate { get; set; }
}

/// <summary>
/// Represents a summary of a project with activity information
/// </summary>
public class ProjectSummary
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int ActivityCount { get; set; }
    public decimal TotalHours { get; set; }
    public DateTime LastActivityDate { get; set; }
}

/// <summary>
/// Represents a charge code option for selection
/// </summary>
public class ChargeCodeOption
{
    public int ChargeCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ValidFromDate { get; set; }
    public DateTime ValidToDate { get; set; }
    public bool HasActivities { get; set; }
}

/// <summary>
/// Represents a charge code report
/// </summary>
public class ChargeCodeReport
{
    public string UserId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<ChargeCodeOption> SelectedChargeCodes { get; set; } = new List<ChargeCodeOption>();
    public List<Activity> Activities { get; set; } = new List<Activity>();
    public List<ChargeCodeSummary> ChargeCodeSummaries { get; set; } = new List<ChargeCodeSummary>();
    public decimal TotalHours { get; set; }
    public DateTime GeneratedDate { get; set; }
}
