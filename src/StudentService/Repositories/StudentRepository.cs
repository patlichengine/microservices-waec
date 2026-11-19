
// Implement the IStudentRepository interface in the StudentRepository class. The StudentRepository class should use the StudentDbContext to interact with the database and perform the necessary operations. The methods should use Entity Framework Core to interact with the database and perform the necessary operations. The methods should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The methods should also use cancellation tokens to allow for graceful cancellation of long-running operations. The methods should also use asynchronous programming techniques to improve performance and scalability of the application. The methods should also use dependency injection to allow for easy testing and mocking of the repository class. The methods should also use logging to provide useful information about the operations being performed and any errors that may occur.
// in a minimalistic way, the StudentRepository class should have methods for creating, updating, deleting and getting students. The methods should take in the necessary parameters and return the appropriate types. The methods should use Entity Framework Core to interact with the database and perform the necessary operations. The methods should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The methods should also use cancellation tokens to allow for graceful cancellation of long-running operations. The methods should also use asynchronous programming techniques to improve performance and scalability of the application. The methods should also use dependency injection to allow for easy testing and mocking of the repository class. The methods should also use logging to provide useful information about the operations being performed and any errors that may occur.
using Microsoft.EntityFrameworkCore;
using StudentService.Data;
using StudentService.Models;

namespace StudentService.Repositories;
public class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _context;

    public StudentRepository(StudentDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.EmailAddress == email, cancellationToken);
    }

    public async Task<IEnumerable<Student>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveAsync(Student student)
    {
        _context.Students.Remove(student);
    }
}

