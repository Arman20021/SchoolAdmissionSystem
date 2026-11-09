
using Admission.BLL.Entities;
using Admission.BLL.Enums;
using Admission.BLL.Interfaces.Repositories;
using Admission.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Update;
using Npgsql;

namespace Admission.DAL.Repositories;

public class AdmissionHoldRepository : IAdmissionHoldRepository
{
    private readonly ApplicationDbContext _context;

    public AdmissionHoldRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(
        HoldCreationStatus Status,
        AdmissionHold? Hold
    )> TryCreateAsync(
        AdmissionHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken
            );

        try
        {
            // 1. Lock the selected class row
            var classRows = await _context.AdmissionClasses
                .FromSqlInterpolated(
                    $"SELECT * FROM admission_classes WHERE id = {hold.ClassId} FOR UPDATE"
                )
                .ToListAsync(cancellationToken);

            var admissionClass = classRows.SingleOrDefault();

            if (admissionClass == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return (
                    HoldCreationStatus.ClassNotFound,
                    null
                );
            }

            var now = DateTimeOffset.UtcNow;

            // 2. Find expired holds
            var expiredHolds = await _context.AdmissionHolds
                .Where(x =>
                    x.ClassId == hold.ClassId &&
                    x.Status == HoldStatus.Held &&
                    x.ExpiresAtUtc <= now
                )
                .ToListAsync(cancellationToken);

            // 3. Update expired holds
            foreach (var expiredHold in expiredHolds)
            {
                expiredHold.Status = HoldStatus.Expired;
            }

            if (expiredHolds.Count > 0)
            {
                await _context.SaveChangesAsync(
                    cancellationToken
                );
            }

            // 4. Check duplicate application
            var alreadyApplied = await _context.AdmissionHolds
                .AnyAsync(x =>
                    x.ClassId == hold.ClassId &&
                    x.StudentId == hold.StudentId &&
                    (
                        x.Status == HoldStatus.Held ||
                        x.Status == HoldStatus.Confirmed
                    ),
                    cancellationToken
                );

            if (alreadyApplied)
            {
                // Commit expired-hold updates before returning.
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCreationStatus.DuplicateApplication,
                    null
                );
            }

            // 5. Count occupied seats
            var occupiedSeats = await _context.AdmissionHolds
                .CountAsync(x =>
                    x.ClassId == hold.ClassId &&
                    (
                        x.Status == HoldStatus.Held ||
                        x.Status == HoldStatus.Confirmed
                    ),
                    cancellationToken
                );

            // 6. Check available capacity
            if (occupiedSeats >= admissionClass.TotalSeats)
            {
                // Preserve expired-hold updates.
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCreationStatus.NoSeatsAvailable,
                    null
                );
            }

            // 7. Create new hold
            await _context.AdmissionHolds.AddAsync(
                hold,
                cancellationToken
            );

            await _context.SaveChangesAsync(
                cancellationToken
            );

            // 8. Commit transaction
            await transaction.CommitAsync(
                cancellationToken
            );

            return (
                HoldCreationStatus.Created,
                hold
            );
        }
        catch (DbUpdateException ex)
            when (
                ex.InnerException is PostgresException pg &&
                pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                pg.ConstraintName ==
                    "ux_admission_holds_class_student_active"
            )
        {
            await transaction.RollbackAsync(
                CancellationToken.None
            );

            return (
                HoldCreationStatus.DuplicateApplication,
                null
            );
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None
            );

            throw;
        }
    }

    public async Task<int> ExpireHoldsAsync(
        DateTimeOffset currentUtcTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.AdmissionHolds
            .Where(x =>
                x.Status == HoldStatus.Held &&
                x.ExpiresAtUtc <= currentUtcTime
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.Status,
                    HoldStatus.Expired
                ),
                cancellationToken
            );
    }

}
