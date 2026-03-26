using System.ComponentModel.DataAnnotations;

namespace Chronocode.Data.Models;

/// <summary>
/// Represents a work authorization artifact (attachment) for a charge code
/// </summary>
public class WorkAuthorizationArtifact
{
    public int Id { get; set; }

    [Required]
    public int ChargeCodeId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ChargeCode ChargeCode { get; set; } = null!;
    public ApplicationUser UploadedByUser { get; set; } = null!;
}
