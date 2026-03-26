using Chronocode.Data;
using Chronocode.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronocode.Services;

/// <summary>
/// Service for handling role-based authorization and project access control
/// </summary>
public class AuthorizationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthorizationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Checks if a user is an admin
    /// </summary>
    public async Task<bool> IsAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        
        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    /// <summary>
    /// Checks if a user can access a specific project (admin, owner, or assigned engineer/manager)
    /// </summary>
    public async Task<bool> CanAccessProjectAsync(string userId, int projectId)
    {
        // Admins can access all projects
        if (await IsAdminAsync(userId))
            return true;

        var project = await _context.Projects
            .Include(p => p.Engineers)
            .Include(p => p.Managers)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
            return false;

        // Project owner can access
        if (project.OwnerId == userId)
            return true;

        // Assigned engineers can access
        if (project.Engineers.Any(e => e.UserId == userId && e.IsActive))
            return true;

        // Assigned managers can access
        if (project.Managers.Any(m => m.UserId == userId && m.IsActive))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a user can manage a project (admin or owner)
    /// </summary>
    public async Task<bool> CanManageProjectAsync(string userId, int projectId)
    {
        // Admins can manage all projects
        if (await IsAdminAsync(userId))
            return true;

        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            return false;

        // Project owner can manage
        return project.OwnerId == userId;
    }

    /// <summary>
    /// Checks if a user can assign engineers to a project (admin or project owner)
    /// </summary>
    public async Task<bool> CanAssignEngineersAsync(string userId, int projectId)
    {
        return await CanManageProjectAsync(userId, projectId);
    }

    /// <summary>
    /// Gets all users who can be assigned as engineers (users with Engineer role or no specific role)
    /// </summary>
    public async Task<List<ApplicationUser>> GetAvailableEngineersAsync()
    {
        var engineers = await _userManager.GetUsersInRoleAsync("Engineer");
        var usersWithoutRoles = await _context.Users
            .Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id))
            .ToListAsync();

        return engineers.Concat(usersWithoutRoles).Distinct().ToList();
    }

    /// <summary>
    /// Gets projects that a user can access
    /// </summary>
    public async Task<List<int>> GetAccessibleProjectIdsAsync(string userId)
    {
        // Admins can access all active projects
        if (await IsAdminAsync(userId))
        {
            return await _context.Projects
                .Where(p => p.IsActive)
                .Select(p => p.Id)
                .ToListAsync();
        }

        // Regular users can only access projects they own or are assigned to
        return await _context.Projects
            .Where(p => p.IsActive && (
                p.OwnerId == userId ||
                p.Engineers.Any(e => e.UserId == userId && e.IsActive) ||
                p.Managers.Any(m => m.UserId == userId && m.IsActive)
            ))
            .Select(p => p.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Ensures default admin user exists and creates one if none exists
    /// </summary>
    public async Task EnsureDefaultAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        // Check if any admin users exist
        var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
        
        if (!adminUsers.Any())
        {
            // Get admin credentials from configuration or use defaults
            var defaultAdminEmail = configuration["DefaultAdmin:Email"] ?? "admin@chronocode.local";
            var defaultAdminPassword = configuration["DefaultAdmin:Password"] ?? "Admin123!";
            var defaultAdminFirstName = configuration["DefaultAdmin:FirstName"] ?? "System";
            var defaultAdminLastName = configuration["DefaultAdmin:LastName"] ?? "Administrator";
            
            var adminUser = new ApplicationUser
            {
                UserName = defaultAdminEmail,
                Email = defaultAdminEmail,
                EmailConfirmed = true,
                FirstName = defaultAdminFirstName,
                LastName = defaultAdminLastName,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, defaultAdminPassword);
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("=== DEFAULT ADMIN USER CREATED ===");
                Console.WriteLine($"Email: {defaultAdminEmail}");
                Console.WriteLine($"Password: {defaultAdminPassword}");
                Console.WriteLine("=====================================");
                Console.WriteLine("IMPORTANT: Please change the password after first login!");
                Console.WriteLine("You can configure custom admin credentials in appsettings.json");
            }
            else
            {
                Console.WriteLine("Failed to create default admin user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
    }

    /// <summary>
    /// Ensures default roles exist
    /// </summary>
    public static async Task EnsureRolesExistAsync(RoleManager<IdentityRole> roleManager)
    {
        // Check if roles exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("Engineer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Engineer"));
        }
        if (!await roleManager.RoleExistsAsync("Manager"))
        {
            await roleManager.CreateAsync(new IdentityRole("Manager"));
        }
    }
}
