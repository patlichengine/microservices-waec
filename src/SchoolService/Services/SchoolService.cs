using Microsoft.Extensions.Logging;
using SchoolService.DTOs;
using SchoolService.Messages;
using SchoolService.Models;
using SchoolService.Repositories;

namespace SchoolService.Services;

public partial class SchoolService(
    ISchoolRepository schoolRepository, 
    ISchoolEventPublisher schoolEventPublisher,
    ILogger<SchoolService> logger) : ISchoolService
{
    private readonly ISchoolRepository _schoolRepository = schoolRepository;
    private readonly ILogger<SchoolService> _logger = logger;
    private readonly ISchoolEventPublisher _schoolEventPublisher = schoolEventPublisher;

    private static SchoolResponseDto MapResponse(School school)
    {
        return new SchoolResponseDto
        {
            Id = school.Id,
            SchoolNo = school.SchoolNo ?? string.Empty,
            SchoolName = school.SchoolName ?? string.Empty,
            SchoolAddress = school.SchoolAddress ?? string.Empty,
            City = school.City ?? string.Empty,
            LgaId = school.LgaId,
            Lga = school.Lga!,
            State = school.Lga?.State!,
            Country = school.Lga?.State?.Country!
        };
    }

    public async Task<SchoolResponseDto?> GetSchoolByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetAsync(id, cancellationToken);
            if (school == null)
            {
                return null;
            }

            return MapResponse(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving school with ID {SchoolId}", id);
            throw;
        }
    }

    public async Task<SchoolResponseDto?> GetSchoolByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByNameAsync(name, cancellationToken);
            if (school == null)
            {
                return null;
            }

            return MapResponse(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving school with name {SchoolName}", name);
            throw;
        }
    }

    public async Task<SchoolResponseDto?> GetSchoolByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetByNumberAsync(number, cancellationToken);
            if (school == null)
            {
                return null;
            }

            return MapResponse(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving school with number {SchoolNumber}", number);
            throw;
        }
    }

    public async Task<IEnumerable<SchoolResponseDto>> ListSchoolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.ListAsync(cancellationToken);
            return schools.Select(MapResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing all schools");
            throw;
        }
    }

    public async Task<IEnumerable<SchoolResponseDto>> ListSchoolsByStateAsync(Guid stateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.ListByStateAsync(stateId, cancellationToken);
            return schools.Select(MapResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing schools for State ID {StateId}", stateId);
            throw;
        }
    }

    public async Task<IEnumerable<SchoolResponseDto>> ListSchoolsByCountryAsync(Guid countryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.ListByCountryAsync(countryId, cancellationToken);
            return schools.Select(MapResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing schools for Country ID {CountryId}", countryId);
            throw;
        }
    }

    public async Task<IEnumerable<SchoolResponseDto>> ListSchoolsByLgaAsync(Guid lgaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schools = await _schoolRepository.ListByLgaAsync(lgaId, cancellationToken);
            return schools.Select(MapResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing schools for LGA ID {LgaId}", lgaId);
            throw;
        }
    }

    public async Task<SchoolResponseDto> CreateSchoolAsync(CreateSchoolDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingByNumber = await _schoolRepository.GetByNumberAsync(dto.SchoolNo, cancellationToken);
            if (existingByNumber != null)
            {
                throw new InvalidOperationException($"A school with number '{dto.SchoolNo}' already exists.");
            }

            var school = new School
            {
                Id = Guid.NewGuid(),
                SchoolNo = dto.SchoolNo,
                SchoolName = dto.SchoolName,
                SchoolAddress = dto.SchoolAddress,
                City = dto.City,
                LgaId = dto.LgaId
            };

            await _schoolRepository.AddAsync(school, cancellationToken);
            await _schoolRepository.SaveChangesAsync(cancellationToken);

            var responseDto = MapResponse(school);

            await _schoolEventPublisher.PublishSchoolCreatedEventAsync(responseDto, cancellationToken);

            // Re-fetch so navigation properties (Lga, State, Country) are populated if included by GetAsync
            var created = await _schoolRepository.GetAsync(school.Id, cancellationToken);
            
            return MapResponse(created ?? school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating school {SchoolName} ({SchoolNo})", dto.SchoolName, dto.SchoolNo);
            throw;
        }
    }

    public async Task<SchoolResponseDto?> UpdateSchoolAsync(Guid id, CreateSchoolDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetAsync(id, cancellationToken);
            if (school == null)
            {
                return null;
            }

            school.SchoolNo = dto.SchoolNo;
            school.SchoolName = dto.SchoolName;
            school.SchoolAddress = dto.SchoolAddress;
            school.City = dto.City;
            school.LgaId = dto.LgaId;

            await _schoolRepository.SaveChangesAsync(cancellationToken);

            var updated = await _schoolRepository.GetAsync(id, cancellationToken);
            return MapResponse(updated ?? school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating school with ID {SchoolId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteSchoolAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var school = await _schoolRepository.GetAsync(id, cancellationToken);
            if (school == null)
            {
                return false;
            }

            _schoolRepository.RemoveAsync(school);
            await _schoolRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting school with ID {SchoolId}", id);
            throw;
        }
    }
}
