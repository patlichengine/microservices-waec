// create a full interface for the student service with methods for creating, updating, deleting and getting students by id, email, listing using async methods and cancellation tokens. The interface should be defined in a separate file called IStudentRepository.cs and should be placed in the Repositories folder. The StudentRepository class should be defined in a separate file called StudentRepository.cs and should also be placed in the Repositories folder. The StudentRepository class should have a constructor that takes in an instance of the StudentDbContext class which will be used to interact with the database. The StudentDbContext class should be defined in a separate file called StudentDbContext.cs and should be placed in the Data folder. The StudentDbContext class should inherit from the DbContext class provided by Entity Framework Core and should contain DbSet properties for each of the entities that will be stored in the database. The entities should be defined in separate files and should be placed in the Models folder. Each entity should have properties that correspond to the columns in the database table that will store the entity's data. The properties should have appropriate data types and validation attributes to ensure that the data is valid before it is saved to the database.

using StudentService.Models;
using StudentService.DTOs;

namespace StudentService.Services;
public interface IStudentService
{
    Task<StudentResponseDto?> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StudentResponseDto?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<StudentResponseDto?> GetStudentByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<IEnumerable<StudentResponseDto>> ListStudentsAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default);
    Task<StudentResponseDto?> UpdateStudentAsync(Guid id, UpdateStudentDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default);
}