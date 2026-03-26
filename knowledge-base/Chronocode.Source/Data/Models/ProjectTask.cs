using System.ComponentModel.DataAnnotations;

namespace Chronocode.Data.Models;

/// <summary>
/// Represents a task within a project
/// </summary>
public class ProjectTask
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsComplete { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
