using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolService.Models;

public class Lga
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string LgaCode { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LgaName { get; set; } = null!;

    [Required]
    public Guid StateId { get; set; }

    [ForeignKey("StateId")]
    public State State { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string CountryCode { get; set; } = null!;

    public Country Country { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<School> Schools { get; set; } = new List<School>();
}
