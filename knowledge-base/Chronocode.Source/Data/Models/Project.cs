using System.ComponentModel.DataAnnotations;

namespace Chronocode.Data.Models;

/// <summary>
/// Represents a project that contains tasks and activities for time tracking
/// </summary>
public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser Owner { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsComplete { get; set; } = false;

    // Navigation properties
    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public virtual ICollection<ChargeCode> ChargeCodes { get; set; } = new List<ChargeCode>();
    public virtual ICollection<ProjectEngineer> Engineers { get; set; } = new List<ProjectEngineer>();
    public virtual ICollection<ProjectManager> Managers { get; set; } = new List<ProjectManager>();
}
