
using System;
namespace StudentService.DTOs;
public class CreateStudentDto
{
    public string StudentNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public Gender Gender { get; set; } = Gender.Other;
    public string SchoolName { get; set; } = "";
    // National Identification Number 
    public string Nin { get; set; } = "";
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow;
}