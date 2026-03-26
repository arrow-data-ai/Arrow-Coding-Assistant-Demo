using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chronocode.Data.Models;

/// <summary>
/// Represents an activity within a task that tracks time and charge codes
/// </summary>
public class Activity
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(3000)]
    public string? Description { get; set; }

    [Required]
    public int TaskId { get; set; }

    public ProjectTask Task { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    [Range(0.1, 24.0, ErrorMessage = "Duration must be between 0.1 and 24.0 hours")]
    public decimal DurationHours { get; set; }

    [Required]
    public DateTime ActivityDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ActivityChargeCode> ActivityChargeCodes { get; set; } = new List<ActivityChargeCode>();
}
