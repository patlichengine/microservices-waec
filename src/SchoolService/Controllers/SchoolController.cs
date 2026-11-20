using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolService.DTOs;
using SchoolService.Services;

namespace SchoolService.Controllers;

//[Authorize]
[ApiController]
[Route("api/schools")]
public class SchoolController(ISchoolService schoolService, ILogger<SchoolController> logger) : ControllerBase
{
    private readonly ISchoolService _schoolService = schoolService;
    private readonly ILogger<SchoolController> _logger = logger;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchoolResponseDto>> GetSchoolByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var school = await _schoolService.GetSchoolByIdAsync(id, cancellationToken);
        if (school == null)
        {
            return NotFound();
        }
        return Ok(school);
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<SchoolResponseDto>> GetSchoolByNameAsync(string name, CancellationToken cancellationToken)
    {
        var school = await _schoolService.GetSchoolByNameAsync(name, cancellationToken);
        if (school == null)
        {
            return NotFound();
        }
        return Ok(school);
    }

    [HttpGet("number/{number}")]
    public async Task<ActionResult<SchoolResponseDto>> GetSchoolByNumberAsync(string number, CancellationToken cancellationToken)
    {
        var school = await _schoolService.GetSchoolByNumberAsync(number, cancellationToken);
        if (school == null)
        {
            return NotFound();
        }
        return Ok(school);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolResponseDto>>> ListSchoolsAsync(CancellationToken cancellationToken)
    {
        var schools = await _schoolService.ListSchoolsAsync(cancellationToken);
        return Ok(schools);
    }

    [HttpGet("state/{stateId:guid}")]
    public async Task<ActionResult<IEnumerable<SchoolResponseDto>>> ListSchoolsByStateAsync(Guid stateId, CancellationToken cancellationToken)
    {
        var schools = await _schoolService.ListSchoolsByStateAsync(stateId, cancellationToken);
        return Ok(schools);
    }

    [HttpGet("country/{countryId:guid}")]
    public async Task<ActionResult<IEnumerable<SchoolResponseDto>>> ListSchoolsByCountryAsync(Guid countryId, CancellationToken cancellationToken)
    {
        var schools = await _schoolService.ListSchoolsByCountryAsync(countryId, cancellationToken);
        return Ok(schools);
    }

    [HttpGet("lga/{lgaId:guid}")]
    public async Task<ActionResult<IEnumerable<SchoolResponseDto>>> ListSchoolsByLgaAsync(Guid lgaId, CancellationToken cancellationToken)
    {
        var schools = await _schoolService.ListSchoolsByLgaAsync(lgaId, cancellationToken);
        return Ok(schools);
    }

    [HttpPost]
    public async Task<ActionResult<SchoolResponseDto>> CreateSchoolAsync(CreateSchoolDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var createdSchool = await _schoolService.CreateSchoolAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetSchoolByIdAsync), new { id = createdSchool.Id }, createdSchool);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Attempted to create a school with an existing school number {SchoolNo}.", dto.SchoolNo);
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SchoolResponseDto>> UpdateSchoolAsync(Guid id, CreateSchoolDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updatedSchool = await _schoolService.UpdateSchoolAsync(id, dto, cancellationToken);
            if (updatedSchool == null)
            {
                return NotFound();
            }
            return Ok(updatedSchool);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Attempted to update school {SchoolId} with an existing school number {SchoolNo}.", id, dto.SchoolNo);
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSchoolAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _schoolService.DeleteSchoolAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
