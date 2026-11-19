//create the class for the student model
using System.ComponentModel.DataAnnotations;

namespace StudentService.Models;
public class Student
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; } = null!;

    [StringLength(200)]
    public string PhoneNumber { get; set; } = "";

    [StringLength(10)]
    //I have a Gender enum to the model
    public Gender Gender { get; set; }
    [StringLength(200)]
    public string SchoolName { get; set; } = "";
    [StringLength(20)]
    public string Nin { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
   
}
