//Add the country model details with the CountryCode, CountryName, PhoneExt, MaxSubjects, MinSubjects properties. The CountryCode property should be a string that represents the unique code for the country. The CountryName property should be a string that represents the name of the country. Both properties should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SchoolService.Models;
public class Country
{
    //define Guid Id property as the primary key for the Country model. The Id property should be a unique identifier for each country and should be generated automatically by the database. The Id property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database. 
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    //define unique CountryCode property as a string that represents the unique code for the country. The CountryCode property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database. The CountryCode property should be required and have a maximum length of 10 characters.
    [Required]
    [MaxLength(10)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string CountryName { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string PhoneExt { get; set; } = null!;

    [Required]
    public int MaxSubjects { get; set; }

    [Required]
    public int MinSubjects { get; set; }

    //add DateTime properties for CreatedAt and UpdatedAt. The CreatedAt property should be set to the current date and time when a new country is created. The UpdatedAt property should be updated to the current date and time whenever the country is updated.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    //States navigation property to allow for easy access to the related data. The States property should be a collection of State objects that are associated with the country. The States property should be initialized to an empty collection in the constructor of the Country class.
    public ICollection<State> States { get; set; } = new List<State>();
}