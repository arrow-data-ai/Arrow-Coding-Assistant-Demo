using System.ComponentModel.DataAnnotations;

namespace Chronocode.Data.Models;

/// <summary>
/// Junction table for Project and Manager (ApplicationUser) many-to-many relationship
/// </summary>
public class ProjectManager
{
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UnassignedDate { get; set; }

    public bool IsActive { get; set; } = true;
}
