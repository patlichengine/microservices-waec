// implement the ISchoolRepository interface with the SchoolServiceDbContext class
using SchoolService.Models;
using SchoolService.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolService.Repositories;

public class SchoolRepository(SchoolDbContext context) : ISchoolRepository
{
    private readonly SchoolDbContext _context = context;

    public async Task<School?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
        .AsNoTracking()
        .Include(s => s.Lga)
            .ThenInclude(l => l!.State)
                .ThenInclude(st => st!.Country)
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        
    }

    // GetByNameAsync
    public async Task<School?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
            .AsNoTracking()
            .Include(s => s.Lga)
                .ThenInclude(l => l.State)
                    .ThenInclude(st => st.Country)
            .FirstOrDefaultAsync(s => s.SchoolName == name, cancellationToken);
    }

    // GetByNumberAsync
    public async Task<School?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
            .AsNoTracking()
            .Include(s => s.Lga)
                .ThenInclude(l => l.State)
                    .ThenInclude(st => st.Country)
            .FirstOrDefaultAsync(s => s.SchoolNo == number, cancellationToken);
    }


    // list all school available in the database
    // todo: add pagination later
    public async Task<IEnumerable<School>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.Schools
        .AsNoTracking()
        .Include(s => s.Lga)
            .ThenInclude(l => l!.State)
                .ThenInclude(st => st!.Country)
        .ToListAsync(cancellationToken);
        //return await _context.Schools.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<School>> ListByLgaAsync(Guid lgaId, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
            .AsNoTracking()
            .Include(c => c.Lga)
                .ThenInclude(l => l.State)
                    .ThenInclude(s => s.Country)
            .Where(c => c.LgaId == lgaId)
            .ToListAsync(cancellationToken);
    }

    // list all school available in the database by state
    public async Task<IEnumerable<School>> ListByStateAsync(Guid stateId, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
            .AsNoTracking()
            .Include(c => c.Lga)
                .ThenInclude(l => l.State)
                    .ThenInclude(s => s.Country)
            .Where(c => c.Lga.StateId == stateId)
            .ToListAsync(cancellationToken);
    }

    // list the schools by country id
    public async Task<IEnumerable<School>> ListByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
    {
        return await _context.Schools
            .AsNoTracking()
            .Include(c => c.Lga)
                .ThenInclude(l => l.State)
                    .ThenInclude(s => s.Country)
            .Where(c => c.Lga.State.CountryId == countryId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(School school, CancellationToken cancellationToken = default)
    {
        await _context.Schools.AddAsync(school, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveAsync(School school)
    {
        _context.Schools.Remove(school);
    }
}