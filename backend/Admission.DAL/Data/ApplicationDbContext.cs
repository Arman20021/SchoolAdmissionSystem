
 
using Admission.BLL.Entities;
using Admission.BLL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Admission.DAL.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Database Tables
    public DbSet<AdmissionClass> AdmissionClasses
        => Set<AdmissionClass>();

    public DbSet<AdmissionHold> AdmissionHolds
        => Set<AdmissionHold>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    
        // Admission Class Configuration
 

        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable(
                "admission_classes",
                table => table.HasCheckConstraint(
                    "ck_admission_classes_total_seats_positive",
                    "\"total_seats\" > 0"
                )
            );

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

    
        // Admission Hold Configuration
     
        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            // Table and Check Constraint
            entity.ToTable(
                "admission_holds",
                table => table.HasCheckConstraint(
                    "ck_admission_holds_student_id_17_digits",
                    "\"student_id\" ~ '^[0-9]{17}$'"
                )
            );

            // Primary Key
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(40)
                .IsRequired();

            // Class Foreign Key
            entity.Property(x => x.ClassId)
                .HasColumnName("class_id")
                .HasMaxLength(40)
                .IsRequired();

            // Student Identity
            entity.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasMaxLength(17)
                .IsRequired();

            // Hold Status
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Created At
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Expiration Time
            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Confirmation Time
            entity.Property(x => x.ConfirmedAtUtc)
                .HasColumnName("confirmed_at_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            // One Class -> Many Holds
            entity.HasOne(x => x.AdmissionClass)
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Partial Unique Index
            // Only HELD and CONFIRMED records
            // are considered active.
            entity.HasIndex(x => new
            {
                x.ClassId,
                x.StudentId
            })
                .IsUnique()
                .HasDatabaseName(
                    "ux_admission_holds_class_student_active"
                )
                .HasFilter(
                    "\"status\" IN ('Held', 'Confirmed')"
                );

            // Class + Status + Expiration Index
            entity.HasIndex(x => new
            {
                x.ClassId,
                x.Status,
                x.ExpiresAtUtc
            })
                .HasDatabaseName(
                    "ix_admission_holds_class_status_expiry"
                );

            // Student Applications Lookup Index
            entity.HasIndex(x => x.StudentId)
                .HasDatabaseName(
                    "ix_admission_holds_student_id"
                );
        });
    }
}
