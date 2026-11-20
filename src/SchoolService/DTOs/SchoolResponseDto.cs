// generate the School response DTO class that represents the School models that we created previously

using SchoolService.Models;

namespace SchoolService.DTOs;

public class SchoolResponseDto
{
    public Guid Id { get; set; }
    public string SchoolNo { get; set; } = null!;
    public string SchoolName { get; set; } = null!;
    public string SchoolAddress { get; set; } = null!;
    public string City { get; set; } = null!;
    public Guid LgaId { get; set; }
    public Country Country { get; set; } = null!;
    public State State { get; set ; } = null!;
    public Lga Lga { get; set; } = null!;
}
