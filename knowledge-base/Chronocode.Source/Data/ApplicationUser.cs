using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Chronocode.Data.Models;

namespace Chronocode.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public virtual ICollection<ProjectEngineer> EngineerAssignments { get; set; } = new List<ProjectEngineer>();
    public virtual ICollection<ProjectManager> ManagerAssignments { get; set; } = new List<ProjectManager>();
    public virtual ICollection<WorkAuthorizationArtifact> UploadedArtifacts { get; set; } = new List<WorkAuthorizationArtifact>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}

