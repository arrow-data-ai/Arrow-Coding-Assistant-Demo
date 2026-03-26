using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Chronocode.Services;

/// <summary>
/// Service for managing projects with role-based access control
/// </summary>
public class ProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthorizationService _authService;

    public ProjectService(ApplicationDbContext context, AuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Gets all projects for a specific user based on their access permissions
    /// </summary>
    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        var accessibleProjectIds = await _authService.GetAccessibleProjectIdsAsync(userId);
        
        return await _context.Projects
            .Where(p => accessibleProjectIds.Contains(p.Id))
            .Include(p => p.Tasks)
            .Include(p => p.ChargeCodes)
            .Include(p => p.Engineers)
                .ThenInclude(e => e.User)
            .Include(p => p.Managers)
                .ThenInclude(m => m.User)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all active and incomplete projects for a specific user based on their access permissions
    /// </summary>
    public async Task<List<Project>> GetUserActiveProjectsAsync(string userId, bool excludeCompleted = true)
    {
        var accessibleProjectIds = await _authService.GetAccessibleProjectIdsAsync(userId);
        
        var query = _context.Projects
            .Where(p => accessibleProjectIds.Contains(p.Id) && p.IsActive);

        if (excludeCompleted)
        {
            query = query.Where(p => !p.IsComplete);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific project by ID if user has access
    /// </summary>
    public async Task<Project?> GetProjectByIdAsync(int projectId, string userId)
    {
        if (!await _authService.CanAccessProjectAsync(userId, projectId))
            return null;

        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.ChargeCodes)
            .Include(p => p.Engineers)
                .ThenInclude(e => e.User)
            .Include(p => p.Managers)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);
    }

    /// <summary>
    /// Creates a new project
    /// </summary>
    public async Task<Project> CreateProjectAsync(Project project)
    {
        project.CreatedDate = DateTime.UtcNow;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Updates an existing project
    /// </summary>
    public async Task<Project> UpdateProjectAsync(Project project)
    {
        var existingProject = await _context.Projects.FindAsync(project.Id);
        if (existingProject == null)
        {
            throw new InvalidOperationException($"Project with ID {project.Id} not found.");
        }

        // Update properties manually to avoid tracking conflicts
        existingProject.Name = project.Name;
        existingProject.Description = project.Description;
        existingProject.IsActive = project.IsActive;
        existingProject.IsComplete = project.IsComplete;
        existingProject.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingProject;
    }

    /// <summary>
    /// Deletes a project (soft delete) - only allows deletion by project owner
    /// </summary>
    public async Task<bool> DeleteProjectAsync(int projectId, string userId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project != null && project.OwnerId == userId)
        {
            project.IsActive = false;
            project.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Toggles the completion status of a project - only allowed by project owner or admin
    /// </summary>
    public async Task<bool> ToggleProjectCompletionAsync(int projectId, string userId)
    {
        if (!await _authService.CanManageProjectAsync(userId, projectId))
            return false;

        var project = await _context.Projects.FindAsync(projectId);
        if (project != null)
        {
            project.IsComplete = !project.IsComplete;
            project.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds an engineer to a project - only allowed by admins or project owners
    /// </summary>
    public async Task<bool> AddEngineerToProjectAsync(int projectId, string engineerUserId, string requestingUserId)
    {
        if (!await _authService.CanAssignEngineersAsync(requestingUserId, projectId))
            return false;

        var existing = await _context.ProjectEngineers
            .FirstOrDefaultAsync(pe => pe.ProjectId == projectId && pe.UserId == engineerUserId);

        if (existing == null)
        {
            _context.ProjectEngineers.Add(new ProjectEngineer
            {
                ProjectId = projectId,
                UserId = engineerUserId,
                AssignedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }
        else if (!existing.IsActive)
        {
            existing.IsActive = true;
            existing.AssignedDate = DateTime.UtcNow;
            existing.UnassignedDate = null;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    /// <summary>
    /// Adds a manager to a project - only allowed by admins or project owners
    /// </summary>
    public async Task<bool> AddManagerToProjectAsync(int projectId, string managerUserId, string requestingUserId)
    {
        if (!await _authService.CanAssignEngineersAsync(requestingUserId, projectId))
            return false;

        var existing = await _context.ProjectManagers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == managerUserId);

        if (existing == null)
        {
            _context.ProjectManagers.Add(new ProjectManager
            {
                ProjectId = projectId,
                UserId = managerUserId,
                AssignedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }
        else if (!existing.IsActive)
        {
            existing.IsActive = true;
            existing.AssignedDate = DateTime.UtcNow;
            existing.UnassignedDate = null;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    /// <summary>
    /// Removes an engineer from a project - only allowed by admins or project owners
    /// </summary>
    public async Task<bool> RemoveEngineerFromProjectAsync(int projectId, string engineerUserId, string requestingUserId)
    {
        if (!await _authService.CanAssignEngineersAsync(requestingUserId, projectId))
            return false;

        var existing = await _context.ProjectEngineers
            .FirstOrDefaultAsync(pe => pe.ProjectId == projectId && pe.UserId == engineerUserId && pe.IsActive);

        if (existing != null)
        {
            existing.IsActive = false;
            existing.UnassignedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all available engineers that can be assigned to projects
    /// </summary>
    public async Task<List<ApplicationUser>> GetAvailableEngineersAsync()
    {
        return await _authService.GetAvailableEngineersAsync();
    }
}
