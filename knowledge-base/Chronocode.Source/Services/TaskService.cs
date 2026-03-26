using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for managing tasks within projects with role-based access control
/// </summary>
public class TaskService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthorizationService _authService;

    public TaskService(ApplicationDbContext context, AuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Gets all tasks for a specific project - only if user has access
    /// </summary>
    public async Task<List<ProjectTask>> GetProjectTasksAsync(int projectId, string userId)
    {
        if (!await _authService.CanAccessProjectAsync(userId, projectId))
            return new List<ProjectTask>();

        return await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId && t.IsActive)
            .Include(t => t.Activities)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific task by ID - only if user has access to the project
    /// </summary>
    public async Task<ProjectTask?> GetTaskByIdAsync(int taskId, string userId)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
            .Include(t => t.Activities)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsActive);

        if (task == null || !await _authService.CanAccessProjectAsync(userId, task.ProjectId))
            return null;

        return task;
    }

    /// <summary>
    /// Creates a new task - only if user can manage the project and project is not completed
    /// </summary>
    public async Task<ProjectTask?> CreateTaskAsync(ProjectTask task, string userId)
    {
        if (!await _authService.CanManageProjectAsync(userId, task.ProjectId))
            return null;

        // Check if the project is completed
        var project = await _context.Projects.FindAsync(task.ProjectId);
        if (project?.IsComplete == true)
        {
            throw new InvalidOperationException("Cannot create tasks in a completed project.");
        }

        task.CreatedDate = DateTime.UtcNow;
        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    /// <summary>
    /// Creates a new task in multiple projects - only if user can manage each project
    /// </summary>
    public async Task<List<ProjectTask>> CreateTaskInMultipleProjectsAsync(ProjectTask task, List<int> projectIds, string userId)
    {
        var createdTasks = new List<ProjectTask>();

        // Fetch all projects in a single query to avoid N+1 problem
        var projects = await _context.Projects
            .Where(p => projectIds.Contains(p.Id) && !p.IsComplete)
            .ToListAsync();

        foreach (var project in projects)
        {
            // Check if user can manage this project
            if (!await _authService.CanManageProjectAsync(userId, project.Id))
                continue;

            // Create a new task for this project
            var newTask = new ProjectTask
            {
                Name = task.Name,
                Description = task.Description,
                ProjectId = project.Id,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                IsComplete = false
            };

            _context.ProjectTasks.Add(newTask);
            createdTasks.Add(newTask);
        }

        await _context.SaveChangesAsync();
        return createdTasks;
    }

    /// <summary>
    /// Updates an existing task - only if user can manage the project
    /// </summary>
    public async Task<bool> UpdateTaskAsync(ProjectTask task, string userId)
    {
        if (!await _authService.CanManageProjectAsync(userId, task.ProjectId))
            return false;

        var existingTask = await _context.ProjectTasks.FindAsync(task.Id);
        if (existingTask == null)
        {
            throw new InvalidOperationException($"Task with ID {task.Id} not found.");
        }

        // Update properties manually to avoid tracking conflicts
        existingTask.Name = task.Name;
        existingTask.Description = task.Description;
        existingTask.IsActive = task.IsActive;
        existingTask.IsComplete = task.IsComplete;
        existingTask.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes a task (soft delete) - only if user can manage the project
    /// </summary>
    public async Task<bool> DeleteTaskAsync(int taskId, string userId)
    {
        var task = await _context.ProjectTasks.FindAsync(taskId);
        if (task == null || !await _authService.CanManageProjectAsync(userId, task.ProjectId))
            return false;

        task.IsActive = false;
        task.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Toggles the completion status of a task - only if user can manage the project
    /// </summary>
    public async Task<bool> ToggleTaskCompletionAsync(int taskId, string userId)
    {
        var task = await _context.ProjectTasks.FindAsync(taskId);
        if (task == null || !await _authService.CanManageProjectAsync(userId, task.ProjectId))
            return false;

        task.IsComplete = !task.IsComplete;
        task.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
