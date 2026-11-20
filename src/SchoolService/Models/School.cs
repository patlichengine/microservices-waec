// add the following properties to the School class: IsDeleted, CreatedAt, UpdatedAt. The IsDeleted property should be a boolean that indicates whether the school has been deleted or not. The CreatedAt property should be a DateTime that indicates when the school was created. The UpdatedAt property should be a DateTime that indicates when the school was last updated. The IsDeleted property should be set to false by default. The CreatedAt and UpdatedAt properties should be set to the current date and time when a new school is created. The UpdatedAt property should be updated to the current date and time whenever the school is updated.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolService.Models;
public class School
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(7)]
    public string SchoolNo { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string SchoolName { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string SchoolAddress { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = null!;

    public Guid LgaId { get; set; }
    // add a foreign key for the Lga model, the field name in the Lga model is Id, the field name in the School model is LgaId. The LgaId property should be a Guid that represents the unique identifier for the local government area in which the school is located. The LgaId property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database.
    
    [ForeignKey("LgaId")] 
    public Lga Lga { get; set; } = null!;

    public bool IsDeleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
