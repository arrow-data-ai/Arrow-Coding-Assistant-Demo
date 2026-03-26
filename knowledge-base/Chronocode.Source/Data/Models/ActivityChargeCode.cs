using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chronocode.Data.Models;

/// <summary>
/// Junction table for Activity and ChargeCode many-to-many relationship with time allocation
/// </summary>
public class ActivityChargeCode
{
    public int Id { get; set; }

    [Required]
    public int ActivityId { get; set; }

    public Activity Activity { get; set; } = null!;

    [Required]
    public int ChargeCodeId { get; set; }

    public ChargeCode ChargeCode { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal AllocatedHours { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }
}
