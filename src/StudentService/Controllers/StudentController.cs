// create the actions for the controller that exposes the student service methods for creating, updating, deleting and getting students by id, email, listing using async methods and cancellation tokens. The controller should be defined in a separate file called StudentController.cs and should be placed in the Controllers folder. The StudentController class should have a constructor that takes in an instance of the IStudentService interface which will be used to interact with the student service. The StudentController class should have methods for each of the actions that will be exposed by the controller. The methods should take in the necessary parameters and return appropriate types. The methods should use the IStudentService interface to interact with the student service and perform the necessary operations. The methods should also handle any exceptions that may occur during the operations and return appropriate error messages to the caller.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentService.DTOs;
using StudentService.Services;

namespace StudentService.Controllers;

//[Authorize]
[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentResponseDto>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await _studentService.GetStudentByIdAsync(id, cancellationToken);
        if (student == null)
        {
            return NotFound();
        }
        return Ok(student);
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<StudentResponseDto>> GetStudentByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var student = await _studentService.GetStudentByEmailAsync(email, cancellationToken);
        if (student == null)
        {
            return NotFound();
        }
        return Ok(student);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentResponseDto>>> ListStudentsAsync(CancellationToken cancellationToken)
    {
        var students = await _studentService.ListStudentsAsync(cancellationToken);
        return Ok(students);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken)
    {
        //enclose with try catch block and add problem conflict response if student with same email already exists
        try
        {
            var id = await _studentService.CreateStudentAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetStudentByIdAsync), new { id }, id);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Attempted to create a student with an existing email.");
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = "A student with the same email already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStudentAsync(Guid id, UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        // change the return type of the UpdateStudentAsync method in the IStudentService interface to return a StudentResponseDto instead of void. This will allow the controller to return the updated student object to the caller. The UpdateStudentAsync method in the StudentService class should also be updated to return a StudentResponseDto instead of void. The method should retrieve the existing student object from the database, update its properties with the values from the UpdateStudentDto object, save the changes to the database, and return the updated student object as a StudentResponseDto.  
        try
        {
            var updatedStudent = await _studentService.UpdateStudentAsync(id, dto, cancellationToken);
            if (updatedStudent == null)
            {
                return NotFound();
            }
            return Ok(updatedStudent);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Attempted to update a student with an existing email.");
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = "A student with the same email already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStudentAsync(Guid id, CancellationToken cancellationToken)
    {
        // track isDeleted in the Student entity and set it to true instead of deleting the record from the database. The GetStudentByIdAsync and GetStudentByEmailAsync methods in the StudentService class should be updated to only return students that are not marked as deleted. The ListStudentsAsync method should also be updated to only return students that are not marked as deleted. The DeleteStudentAsync method in the StudentService class should be updated to mark the student as deleted instead of removing it from the database. The method should retrieve the existing student object from the database, set its IsDeleted property to true, save the changes to the database, and return a boolean indicating whether the operation was successful or not. The controller should handle any exceptions that may occur during the operation and return appropriate error messages to the caller.

        var result = await _studentService.DeleteStudentAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}