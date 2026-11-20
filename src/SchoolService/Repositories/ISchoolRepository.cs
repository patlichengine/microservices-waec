//create a full interface for the student service with methods for creating, updating, deleting and getting students
using SchoolService.Models;
namespace SchoolService.Repositories;

public interface ISchoolRepository
{
    Task<School?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<School?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<School?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<IEnumerable<School>> ListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<School>> ListByStateAsync(Guid StateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<School>> ListByCountryAsync(Guid CountryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<School>> ListByLgaAsync(Guid LgaId, CancellationToken cancellationToken = default);
    Task AddAsync(School dto, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void RemoveAsync(School dto);
}