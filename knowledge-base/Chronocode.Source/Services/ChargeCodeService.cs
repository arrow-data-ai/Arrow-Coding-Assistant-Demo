using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for managing charge codes
/// </summary>
public class ChargeCodeService
{
    private readonly ApplicationDbContext _context;

    public ChargeCodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all charge codes for a specific project
    /// </summary>
    public async Task<List<ChargeCode>> GetProjectChargeCodesAsync(int projectId)
    {
        return await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId && cc.IsActive)
            .OrderBy(cc => cc.Code)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all charge codes for a specific project (including inactive ones for management purposes)
    /// </summary>
    public async Task<List<ChargeCode>> GetAllProjectChargeCodesAsync(int projectId)
    {
        return await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId)
            .OrderBy(cc => cc.Code)
            .ToListAsync();
    }

    /// <summary>
    /// Gets active charge codes for a specific project on a given date
    /// </summary>
    public async Task<List<ChargeCode>> GetActiveChargeCodesAsync(int projectId, DateTime date)
    {
        return await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId && 
                        cc.IsActive &&
                        cc.ValidFromDate <= date &&
                        cc.ValidToDate >= date)
            .OrderBy(cc => cc.Code)
            .ToListAsync();
    }

    /// <summary>
    /// Gets charge codes that are about to expire (within specified days)
    /// </summary>
    public async Task<List<ChargeCode>> GetExpiringChargeCodesAsync(int projectId, int daysAhead = 30)
    {
        var cutoffDate = DateTime.Today.AddDays(daysAhead);
        return await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId && 
                        cc.IsActive &&
                        cc.ValidToDate <= cutoffDate &&
                        cc.ValidToDate >= DateTime.Today)
            .OrderBy(cc => cc.ValidToDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific charge code by ID
    /// </summary>
    public async Task<ChargeCode?> GetChargeCodeByIdAsync(int chargeCodeId)
    {
        return await _context.ChargeCodes
            .Include(cc => cc.Project)
            .Include(cc => cc.WorkAuthorizationArtifacts.Where(waa => waa.IsActive))
            .FirstOrDefaultAsync(cc => cc.Id == chargeCodeId);
    }

    /// <summary>
    /// Creates a new charge code
    /// </summary>
    public async Task<ChargeCode> CreateChargeCodeAsync(ChargeCode chargeCode)
    {
        chargeCode.CreatedDate = DateTime.UtcNow;
        _context.ChargeCodes.Add(chargeCode);
        await _context.SaveChangesAsync();
        return chargeCode;
    }

    /// <summary>
    /// Updates an existing charge code
    /// </summary>
    public async Task<ChargeCode> UpdateChargeCodeAsync(ChargeCode chargeCode)
    {
        var existingChargeCode = await _context.ChargeCodes.FindAsync(chargeCode.Id);
        if (existingChargeCode == null)
        {
            throw new InvalidOperationException($"ChargeCode with ID {chargeCode.Id} not found.");
        }

        // Update properties manually to avoid tracking conflicts
        existingChargeCode.Code = chargeCode.Code;
        existingChargeCode.Name = chargeCode.Name;
        existingChargeCode.Description = chargeCode.Description;
        existingChargeCode.ValidFromDate = chargeCode.ValidFromDate;
        existingChargeCode.ValidToDate = chargeCode.ValidToDate;
        existingChargeCode.IsActive = chargeCode.IsActive;
        existingChargeCode.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingChargeCode;
    }

    /// <summary>
    /// Deletes a charge code (soft delete)
    /// </summary>
    public async Task DeleteChargeCodeAsync(int chargeCodeId)
    {
        var chargeCode = await _context.ChargeCodes.FindAsync(chargeCodeId);
        if (chargeCode != null)
        {
            chargeCode.IsActive = false;
            chargeCode.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Toggles the active status of a charge code
    /// </summary>
    public async Task<bool> ToggleChargeCodeStatusAsync(int chargeCodeId)
    {
        var chargeCode = await _context.ChargeCodes.FindAsync(chargeCodeId);
        if (chargeCode != null)
        {
            chargeCode.IsActive = !chargeCode.IsActive;
            chargeCode.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return chargeCode.IsActive;
        }
        return false;
    }

    /// <summary>
    /// Gets count of active and inactive charge codes for a project
    /// </summary>
    public async Task<(int Active, int Inactive)> GetChargeCodeCountsAsync(int projectId)
    {
        var chargeCodes = await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId)
            .ToListAsync();
        
        var active = chargeCodes.Count(cc => cc.IsActive);
        var inactive = chargeCodes.Count(cc => !cc.IsActive);
        
        return (active, inactive);
    }

    /// <summary>
    /// Validates if a charge code is active for a given date
    /// </summary>
    public async Task<bool> IsChargeCodeValidAsync(int chargeCodeId, DateTime date)
    {
        var chargeCode = await _context.ChargeCodes.FindAsync(chargeCodeId);
        return chargeCode != null && 
               chargeCode.IsActive && 
               chargeCode.ValidFromDate <= date && 
               chargeCode.ValidToDate >= date;
    }

    /// <summary>
    /// Checks if a charge code can be safely deleted (has no activities)
    /// </summary>
    public async Task<bool> CanDeleteChargeCodeAsync(int chargeCodeId)
    {
        var hasActivities = await _context.ActivityChargeCodes
            .AnyAsync(acc => acc.ChargeCodeId == chargeCodeId);
        return !hasActivities;
    }

    /// <summary>
    /// Gets the count of activities using a charge code
    /// </summary>
    public async Task<int> GetChargeCodeActivityCountAsync(int chargeCodeId)
    {
        return await _context.ActivityChargeCodes
            .Where(acc => acc.ChargeCodeId == chargeCodeId)
            .CountAsync();
    }

    /// <summary>
    /// Gets the last date a charge code was used in an activity
    /// </summary>
    public async Task<DateTime?> GetChargeCodeLastUsedDateAsync(int chargeCodeId)
    {
        var lastActivity = await _context.ActivityChargeCodes
            .Where(acc => acc.ChargeCodeId == chargeCodeId)
            .Include(acc => acc.Activity)
            .OrderByDescending(acc => acc.Activity.ActivityDate)
            .FirstOrDefaultAsync();

        return lastActivity?.Activity.ActivityDate;
    }

    /// <summary>
    /// Hard deletes a charge code (only if no activities)
    /// </summary>
    /// <returns>True if deleted, false if has activities or not found</returns>
    public async Task<bool> HardDeleteChargeCodeAsync(int chargeCodeId)
    {
        if (!await CanDeleteChargeCodeAsync(chargeCodeId))
        {
            return false; // Cannot delete - has activities
        }

        var chargeCode = await _context.ChargeCodes
            .Include(cc => cc.WorkAuthorizationArtifacts)
            .FirstOrDefaultAsync(cc => cc.Id == chargeCodeId);
            
        if (chargeCode != null)
        {
            // Remove related work authorization artifacts first
            if (chargeCode.WorkAuthorizationArtifacts.Any())
            {
                _context.WorkAuthorizationArtifacts.RemoveRange(chargeCode.WorkAuthorizationArtifacts);
            }
            
            _context.ChargeCodes.Remove(chargeCode);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all charge codes for a project that have no activities
    /// </summary>
    public async Task<List<ChargeCode>> GetUnusedChargeCodesAsync(int projectId)
    {
        var allChargeCodes = await _context.ChargeCodes
            .Where(cc => cc.ProjectId == projectId)
            .ToListAsync();

        var usedChargeCodeIds = await _context.ActivityChargeCodes
            .Select(acc => acc.ChargeCodeId)
            .Distinct()
            .ToListAsync();

        return allChargeCodes
            .Where(cc => !usedChargeCodeIds.Contains(cc.Id))
            .OrderBy(cc => cc.Code)
            .ToList();
    }
}
