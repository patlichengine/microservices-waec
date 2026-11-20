using Microsoft.EntityFrameworkCore;
using SchoolService.Models;

namespace SchoolService.Data;

public class SchoolDbContext : DbContext
{
    public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<State> States { get; set; } = null!;
    public DbSet<Lga> Lgas { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Country
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.CountryCode).IsRequired().HasMaxLength(10);
            entity.Property(c => c.CountryName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.PhoneExt).IsRequired().HasMaxLength(10);
            entity.Property(c => c.MaxSubjects).IsRequired();
            entity.Property(c => c.MinSubjects).IsRequired();

            entity.HasIndex(c => c.CountryCode).IsUnique();
            entity.HasIndex(c => c.CountryName).IsUnique();

            entity.HasMany(c => c.States)
                .WithOne(s => s.Country)
                .HasForeignKey(s => s.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 2. State
        modelBuilder.Entity<State>(entity =>
        {
            entity.ToTable("States");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.StateCode).IsRequired().HasMaxLength(10);
            entity.Property(s => s.StateName).IsRequired().HasMaxLength(100);

            entity.HasIndex(s => new { s.CountryId, s.StateCode }).IsUnique();
            entity.HasIndex(s => new { s.CountryId, s.StateName }).IsUnique();

            entity.HasMany(s => s.Lgas)
                .WithOne(l => l.State)
                .HasForeignKey(l => l.StateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Lga
        modelBuilder.Entity<Lga>(entity =>
        {
            entity.ToTable("Lgas");
            entity.HasKey(l => l.Id);

            entity.Property(l => l.LgaCode).IsRequired().HasMaxLength(10);
            entity.Property(l => l.LgaName).IsRequired().HasMaxLength(100);
            entity.Property(l => l.CountryCode).IsRequired().HasMaxLength(10);

            entity.HasIndex(l => new { l.StateId, l.LgaCode }).IsUnique();
            entity.HasIndex(l => new { l.StateId, l.LgaName }).IsUnique();

            // Foreign key from Lga.CountryCode to Country.CountryCode (alternate unique key)
            entity.HasOne(l => l.Country)
                .WithMany()
                .HasForeignKey(l => l.CountryCode)
                .HasPrincipalKey(c => c.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(l => l.Schools)
                .WithOne(s => s.Lga)
                .HasForeignKey(s => s.LgaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. School
        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("Schools");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.SchoolNo).IsRequired().HasMaxLength(7);
            entity.Property(s => s.SchoolName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.SchoolAddress).IsRequired().HasMaxLength(200);
            entity.Property(s => s.City).IsRequired().HasMaxLength(100);
            entity.Property(s => s.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(s => s.SchoolNo).IsUnique();
            entity.HasIndex(s => s.SchoolName);

            // Exclude soft-deleted schools from standard queries
            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        // 5. Deterministic Seed Data (Static GUIDs & Timestamps required for EF Migrations)
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var nigeriaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lagosStateId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var lagosMainlandId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<Country>().HasData(
            new Country
            {
                Id = nigeriaId,
                CountryCode = "NG",
                CountryName = "Nigeria",
                PhoneExt = "+234",
                MaxSubjects = 9,
                MinSubjects = 7,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        modelBuilder.Entity<State>().HasData(
            new State
            {
                Id = lagosStateId,
                StateCode = "LA",
                StateName = "Lagos",
                CountryId = nigeriaId,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        modelBuilder.Entity<Lga>().HasData(
            new Lga
            {
                Id = lagosMainlandId,
                LgaCode = "LA-ML",
                LgaName = "Lagos Mainland",
                StateId = lagosStateId,
                CountryCode = "NG",
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                    entry.Property("CreatedAt").CurrentValue = utcNow;
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
