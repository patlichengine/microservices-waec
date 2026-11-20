using Microsoft.EntityFrameworkCore;
using StudentService.Models;
namespace StudentService.Data;

public class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options)
        : base(options)
    {
    }

    // DbSet for Students
    public DbSet<Student> Students { get; set; } = null!;

    // add the complete model for the student to the database
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // create the model for the student
        base.OnModelCreating(modelBuilder);


        // define the table name for the student model
        modelBuilder.Entity<Student>().ToTable("Students");

        //define the properties for the student model
        modelBuilder.Entity<Student>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentNumber).IsRequired().HasMaxLength(10);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.SchoolName).HasMaxLength(200);
            entity.Property(e => e.Nin).HasMaxLength(20);

            // Enforce unique email (and NIN if applicable) at the DB level
            entity.HasIndex(e => e.StudentNumber).IsUnique();
            entity.HasIndex(e => e.EmailAddress).IsUnique();
            entity.HasIndex(e => e.Nin).IsUnique();

            // Automatically filter out soft-deleted students on queries
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        

        // what next? I want to add a seeding data for the student model
        // add seeding data for the student model multiple at once  
        // modelBuilder.Entity<Student>().HasData(
        //     new Student
        //     {
        //         Id = Guid.NewGuid(),
        //         FirstName = "John",
        //         LastName = "Doe",
        //         EmailAddress = "john.doe@example.com",
        //         PhoneNumber = "123-456-7890",
        //         Gender = Gender.Male,
        //         SchoolName = "Example School",
        //         Nin = "123456789"
        //     }
        // );
        // //Add more seeding data for the student model
        // modelBuilder.Entity<Student>().HasData(
        //     new Student
        //     {
        //         Id = Guid.NewGuid(),
        //         FirstName = "Jane",
        //         LastName = "Smith",
        //         EmailAddress = "jane.smith@example.com",
        //         PhoneNumber = "098-765-4321",
        //         Gender = Gender.Female,
        //         SchoolName = "Another School",
        //         Nin = "987654321"
        //     }
        // );

    }

    // what next? I want to add a seeding data for the student model 

}
