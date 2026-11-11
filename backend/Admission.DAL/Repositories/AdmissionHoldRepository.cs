
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
            // 1. Lock this student across all application requests.
            // This prevents simultaneous applications to different classes.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({hold.StudentId}, 0))",
                cancellationToken
            );

            // 2. Lock the selected class to protect seat capacity.
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

            // 3. Expire ALL expired holds belonging to this student,
            // regardless of which class they belong to.
            await _context.AdmissionHolds
                .Where(x =>
                    x.StudentId == hold.StudentId &&
                    x.Status == HoldStatus.Held &&
                    x.ExpiresAtUtc <= now
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.Status,
                        HoldStatus.Expired
                    ),
                    cancellationToken
                );

            // 4. Check whether this student has an active
            // application in ANY class.
            var alreadyApplied = await _context.AdmissionHolds
                .AnyAsync(x =>
                    x.StudentId == hold.StudentId &&
                    (
                        x.Status == HoldStatus.Held ||
                        x.Status == HoldStatus.Confirmed
                    ),
                    cancellationToken
                );

            if (alreadyApplied)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCreationStatus.DuplicateApplication,
                    null
                );
            }

            // 5. Count occupied seats in the selected class.
            var occupiedSeats = await _context.AdmissionHolds
                .CountAsync(x =>
                    x.ClassId == hold.ClassId &&
                    (
                        x.Status == HoldStatus.Held ||
                        x.Status == HoldStatus.Confirmed
                    ),
                    cancellationToken
                );

            // 6. Check available capacity.
            if (occupiedSeats >= admissionClass.TotalSeats)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCreationStatus.NoSeatsAvailable,
                    null
                );
            }

            // 7. Create the new hold.
            await _context.AdmissionHolds.AddAsync(
                hold,
                cancellationToken
            );

            await _context.SaveChangesAsync(cancellationToken);

            // 8. Commit transaction.
            await transaction.CommitAsync(cancellationToken);

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

    public async Task<(
        HoldConfirmationStatus Status,
        AdmissionHold? Hold
    )> TryConfirmAsync(
        string holdId,
        string studentId,
        DateTimeOffset currentUtcTime,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken
            );

        try
        {
            // 1. Lock the selected hold row
            var holdRows = await _context.AdmissionHolds
                .FromSqlInterpolated(
                    $"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE"
                )
                .AsTracking()
                .ToListAsync(cancellationToken);

            var hold = holdRows.SingleOrDefault();

            // 2. Check whether hold exists
            if (hold == null)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldConfirmationStatus.NotFound,
                    null
                );
            }

            // 3. Verify student ownership
            if (hold.StudentId != studentId)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldConfirmationStatus.Forbidden,
                    null
                );
            }

            // 4. Check current hold status
            switch (hold.Status)
            {
                case HoldStatus.Confirmed:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldConfirmationStatus.AlreadyConfirmed,
                        hold
                    );

                case HoldStatus.Expired:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldConfirmationStatus.Expired,
                        hold
                    );

                case HoldStatus.Cancelled:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldConfirmationStatus.Cancelled,
                        hold
                    );
            }

            // 5. Check hold expiration
            if (hold.ExpiresAtUtc <= currentUtcTime)
            {
                hold.Status = HoldStatus.Expired;

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                await transaction.CommitAsync(
                    cancellationToken
                );

                return (
                    HoldConfirmationStatus.Expired,
                    hold
                );
            }

            // 6. Confirm the hold
            hold.Status = HoldStatus.Confirmed;

            hold.ConfirmedAtUtc = currentUtcTime;

            await _context.SaveChangesAsync(
                cancellationToken
            );

            // 7. Commit transaction
            await transaction.CommitAsync(
                cancellationToken
            );

            return (
                HoldConfirmationStatus.Confirmed,
                hold
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


    public async Task<(
        HoldLookupStatus Status,
        AdmissionHold? Hold
    )> TryGetByIdAsync(
        string holdId,
        string studentId,
        DateTimeOffset currentUtcTime,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken
            );

        try
        {
            // 1. Lock the selected hold row
            var holdRows = await _context.AdmissionHolds
                .FromSqlInterpolated(
                    $"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE"
                )
                .AsTracking()
                .ToListAsync(cancellationToken);

            var hold = holdRows.SingleOrDefault();

            // 2. Check whether hold exists
            if (hold == null)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldLookupStatus.NotFound,
                    null
                );
            }

            // 3. Verify student ownership
            if (hold.StudentId != studentId)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldLookupStatus.Forbidden,
                    null
                );
            }

            // 4. Check hold expiration
            if (
                hold.Status == HoldStatus.Held &&
                hold.ExpiresAtUtc <= currentUtcTime
            )
            {
                hold.Status = HoldStatus.Expired;

                await _context.SaveChangesAsync(
                    cancellationToken
                );
            }

            // 5. Commit transaction
            await transaction.CommitAsync(
                cancellationToken
            );

            // 6. Return hold details
            return (
                HoldLookupStatus.Found,
                hold
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




    public async Task<(
        HoldCancellationStatus Status,
        AdmissionHold? Hold
    )> TryCancelAsync(
        string holdId,
        string studentId,
        DateTimeOffset currentUtcTime,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken
            );

        try
        {
            // 1. Lock the selected hold row
            var holdRows = await _context.AdmissionHolds
                .FromSqlInterpolated(
                    $"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE"
                )
                .AsTracking()
                .ToListAsync(cancellationToken);

            var hold = holdRows.SingleOrDefault();

            // 2. Check whether hold exists
            if (hold == null)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCancellationStatus.NotFound,
                    null
                );
            }

            // 3. Verify student ownership
            if (hold.StudentId != studentId)
            {
                await transaction.CommitAsync(cancellationToken);

                return (
                    HoldCancellationStatus.Forbidden,
                    null
                );
            }

            // 4. Check current hold status
            switch (hold.Status)
            {
                case HoldStatus.Confirmed:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldCancellationStatus.AlreadyConfirmed,
                        hold
                    );

                case HoldStatus.Expired:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldCancellationStatus.Expired,
                        hold
                    );

                case HoldStatus.Cancelled:

                    await transaction.CommitAsync(cancellationToken);

                    return (
                        HoldCancellationStatus.AlreadyCancelled,
                        hold
                    );
            }

            // 5. Check hold expiration
            if (hold.ExpiresAtUtc <= currentUtcTime)
            {
                hold.Status = HoldStatus.Expired;

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                await transaction.CommitAsync(
                    cancellationToken
                );

                return (
                    HoldCancellationStatus.Expired,
                    hold
                );
            }

            // 6. Cancel the active hold
            hold.Status = HoldStatus.Cancelled;

            await _context.SaveChangesAsync(
                cancellationToken
            );

            // 7. Commit transaction
            await transaction.CommitAsync(
                cancellationToken
            );

            return (
                HoldCancellationStatus.Cancelled,
                hold
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



    public async Task<IReadOnlyList<(AdmissionHold Hold, string ClassName)>>
        GetByStudentIdAsync(
            string studentId,
            DateTimeOffset currentUtcTime,
            CancellationToken cancellationToken = default)
    {
        // 1. Expire the student's expired holds
        await _context.AdmissionHolds
            .Where(x =>
                x.StudentId == studentId &&
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

        // 2. Join AdmissionHolds with AdmissionClasses
        var results = await (
            from hold in _context.AdmissionHolds.AsNoTracking()
            join admissionClass in _context.AdmissionClasses.AsNoTracking()
                on hold.ClassId equals admissionClass.Id
            where hold.StudentId == studentId
            orderby hold.CreatedAtUtc descending
            select new
            {
                Hold = hold,
                ClassName = admissionClass.Name
            }
        ).ToListAsync(cancellationToken);

        // 3. Return hold and class name
        return results
            .Select(x => (x.Hold, x.ClassName))
            .ToList();
    }
}



