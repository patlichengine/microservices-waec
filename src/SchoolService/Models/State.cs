// add state model details with the StateCode, StateName and CountryCode properties. 
// The StateCode property should be a string that represents the unique code for 
// the state. The StateName property should be a string that represents the 
// name of the state. 
// The CountryCode property sould come fron the Country model. All properties should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SchoolService.Models;
public class State
{

    //define Guid Id property as the primary key for the State model. The Id property should be a unique identifier for each state and should be generated automatically by the database. The Id property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database. 
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    //define unique StateCode property as a string that represents the unique code for the state. The StateCode property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database. The StateCode property should be required and have a maximum length of 10 characters.
    [Required]
    [MaxLength(10)]
    public string StateCode { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string StateName { get; set; } = null!;

    // add the countryId field
    public Guid CountryId { get; set; }

    // The CountryId should be a foreign key that references the Country model. The CountryId property should be a Guid that represents the unique identifier for the country in which the state is located. The CountryId property should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database
    [ForeignKey("CountryId")]
    public Country Country { get; set; } = null!;

    //add DateTime properties for CreatedAt and UpdatedAt. The CreatedAt property should be set to the current date and time when a new state is created. The UpdatedAt property should be updated to the current date and time whenever the state is updated.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // add the Lga collection to the class
    public ICollection<Lga> Lgas { get; set; } = new List<Lga>();


}
