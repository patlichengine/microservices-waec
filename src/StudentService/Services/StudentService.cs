    // implement the IStudentService interface in a full way and create a MapResponse method to be reused in the StudentService class. The MapResponse method should take in a Student object and return a StudentResponseDto object. The method should map the properties of the Student object to the properties of the StudentResponseDto object. The method should also handle any null values and return appropriate default values for the properties of the StudentResponseDto object. The method should be defined as a private static method in the StudentService class and should be called from the GetStudentByIdAsync and GetStudentByEmailAsync methods to map the Student objects returned from the database to StudentResponseDto objects before returning them to the caller.
    using StudentService.Models;
    using StudentService.DTOs;
    using StudentService.Repositories;
using StudentService.Messaging;

namespace StudentService.Services;
    public partial class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentEventPublisher _eventPublisher;

        public StudentService(IStudentRepository studentRepository, IStudentEventPublisher eventPublisher)
        {
            _studentRepository = studentRepository;
            _eventPublisher = eventPublisher;
        }

        // this method maps a Student object to a StudentResponseDto object. The method should handle any null values and return appropriate default values for the properties of the StudentResponseDto object. The method should be defined as a private static method in the StudentService class and should be called from the GetStudentByIdAsync and GetStudentByEmailAsync methods to map the Student objects returned from the database to StudentResponseDto objects before returning them to the caller.
        private static StudentResponseDto MapResponse(Student student)
        {
            return new StudentResponseDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                EmailAddress = student.EmailAddress,
                PhoneNumber = student.PhoneNumber,
                Gender = student.Gender,
                SchoolName = student.SchoolName,
                Nin = student.Nin,
                DateOfBirth = student.DateOfBirth
            };
        }

        // this method gets a student by id and returns a StudentResponseDto object. The method should use the GetAsync method of the IStudentRepository interface to get the student from the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<StudentResponseDto?> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetAsync(id, cancellationToken);
            if (student == null)
            {
                return null;
            }

            return MapResponse(student);
        }

        // this method gets a student by email and returns a StudentResponseDto object. The method should use the GetByEmailAsync method of the IStudentRepository interface to get the student from the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<StudentResponseDto?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetByEmailAsync(email, cancellationToken);
            if (student == null)
            {
                return null;
            }

            return MapResponse(student);
        }

        // this method lists all students and returns a list of StudentResponseDto objects. The method should use the ListAsync method of the IStudentRepository interface to get the students from the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<IEnumerable<StudentResponseDto>> ListStudentsAsync(CancellationToken cancellationToken = default)
        {
            var students = await _studentRepository.ListAsync(cancellationToken);
            return students.Select(MapResponse);
        }

        // this method creates a new student and returns the id of the newly created student. The method should use the AddAsync method of the IStudentRepository interface to add the student to the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<Guid> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default)
        {   
            // normalize the email address to lower case to ensure uniqueness
            var normalizedEmail = dto.EmailAddress.ToLowerInvariant();

            // check for existing student with the same email
            var existingStudent = await _studentRepository.GetByEmailAsync(normalizedEmail);
            if (existingStudent != null)
            {
                throw new InvalidOperationException($"A student with the email {normalizedEmail} already exists.");
            }

            // create a new student object and add it to the database
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                EmailAddress = normalizedEmail,
                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender,
                SchoolName = dto.SchoolName,
                Nin = dto.Nin,
                DateOfBirth = dto.DateOfBirth
            };

            await _studentRepository.AddAsync(student, cancellationToken);
            await _studentRepository.SaveChangesAsync(cancellationToken);

            var responseDto = MapResponse(student);

            await _eventPublisher.PublishStudentCreatedEventAsync(responseDto, cancellationToken);


            return student.Id;
        }

        // this method updates an existing student and returns a boolean indicating whether the operation was successful. The method should use the GetAsync method of the IStudentRepository interface to get the student from the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<StudentResponseDto?> UpdateStudentAsync(Guid id, UpdateStudentDto dto, CancellationToken cancellationToken = default)
        {
            // normalize the email address to lower case to ensure uniqueness
            var normalizedEmail = dto.EmailAddress.ToLowerInvariant();

            // check for existing student with the same email, excluding the current student
            var existingStudent = await _studentRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            
            // ensure that the student exists before updating
            if (existingStudent != null && existingStudent.Id == id)
            {
                throw new InvalidOperationException($"A student with the email {normalizedEmail} does not exist.");
            }

            var student = await _studentRepository.GetAsync(id, cancellationToken);
            if (student == null)
            {
                throw new KeyNotFoundException($"Student with ID {id} not found.");
            }

            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            //student.EmailAddress = normalizedEmail;
            student.PhoneNumber = dto.PhoneNumber;
            student.Gender = dto.Gender;
            student.SchoolName = dto.SchoolName;
            student.Nin = dto.Nin;
            student.DateOfBirth = dto.DateOfBirth;

            await _studentRepository.SaveChangesAsync(cancellationToken);
        
            return MapResponse(student);
        }

        // Remove a student by id and return a boolean indicating whether the operation was successful. The method should use the GetAsync method of the IStudentRepository interface to get the student from the database. The method should also handle any exceptions that may occur during the database operations and return appropriate error messages to the caller. The method should also use cancellation tokens to allow for graceful cancellation of long-running operations. The method should also use asynchronous programming techniques to improve performance and scalability of the application. The method should also use dependency injection to allow for easy testing and mocking of the repository class. The method should also use logging to provide useful information about the operations being performed and any errors that may occur.
        public async Task<bool> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetAsync(id, cancellationToken);
            if (student == null)
            {
                return false;
            }

            _studentRepository.RemoveAsync(student);
            await _studentRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }