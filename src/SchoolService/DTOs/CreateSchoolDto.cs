// Create a CreateSchoolDto class that represents the School models that we created previously
namespace SchoolService.DTOs;

public class CreateSchoolDto
{
    //public Guid Id { get; set; }
    public string SchoolNo { get; set; } = null!;
    public string SchoolName { get; set; } = null!;

    public string SchoolAddress { get; set; } = null!;
    public string City { get; set; } = null!;
    public Guid LgaId { get; set; }
}