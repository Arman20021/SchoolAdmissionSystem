
using Admission.BLL.Entites;
 
using Microsoft.EntityFrameworkCore;

namespace Admission.DAL.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdmissionClass> AdmissionClasses
        => Set<AdmissionClass>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}
