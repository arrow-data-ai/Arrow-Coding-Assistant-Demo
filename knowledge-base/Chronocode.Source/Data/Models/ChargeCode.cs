using System.ComponentModel.DataAnnotations;

namespace Chronocode.Data.Models;

/// <summary>
/// Represents a charge code that can be associated with activities
/// </summary>
public class ChargeCode
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    [Required]
    public DateTime ValidFromDate { get; set; }

    [Required]
    public DateTime ValidToDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ActivityChargeCode> ActivityChargeCodes { get; set; } = new List<ActivityChargeCode>();
    public virtual ICollection<WorkAuthorizationArtifact> WorkAuthorizationArtifacts { get; set; } = new List<WorkAuthorizationArtifact>();
}
