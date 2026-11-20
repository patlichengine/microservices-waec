using SchoolService.DTOs;

namespace SchoolService.Services;

public interface ISchoolService
{
    Task<SchoolResponseDto?> GetSchoolByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolResponseDto?> GetSchoolByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<SchoolResponseDto?> GetSchoolByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<IEnumerable<SchoolResponseDto>> ListSchoolsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SchoolResponseDto>> ListSchoolsByStateAsync(Guid stateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SchoolResponseDto>> ListSchoolsByCountryAsync(Guid countryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SchoolResponseDto>> ListSchoolsByLgaAsync(Guid lgaId, CancellationToken cancellationToken = default);
    Task<SchoolResponseDto> CreateSchoolAsync(CreateSchoolDto dto, CancellationToken cancellationToken = default);
    Task<SchoolResponseDto?> UpdateSchoolAsync(Guid id, CreateSchoolDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteSchoolAsync(Guid id, CancellationToken cancellationToken = default);
}
