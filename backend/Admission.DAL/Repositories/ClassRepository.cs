
using Admission.BLL.DTOs.Classes;
using Admission.BLL.Entities;
using Admission.BLL.Enums;
using Admission.BLL.Interfaces.Repositories;
using Admission.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Admission.DAL.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly ApplicationDbContext _context;

    public ClassRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        AdmissionClass admissionClass,
        CancellationToken cancellationToken = default)
    {
        await _context.AdmissionClasses.AddAsync(
            admissionClass,
            cancellationToken
        );

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUtcTime = DateTimeOffset.UtcNow;

        return await BuildClassResponseQuery(currentUtcTime)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassResponseDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var currentUtcTime = DateTimeOffset.UtcNow;

        return await BuildClassResponseQuery(currentUtcTime)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );
    }

    private IQueryable<ClassResponseDto> BuildClassResponseQuery(
        DateTimeOffset currentUtcTime)
    {
        return
            from admissionClass in _context.AdmissionClasses.AsNoTracking()

            let heldSeats = _context.AdmissionHolds.Count(hold =>
                hold.ClassId == admissionClass.Id &&
                hold.Status == HoldStatus.Held &&
                hold.ExpiresAtUtc > currentUtcTime
            )

            let confirmedSeats = _context.AdmissionHolds.Count(hold =>
                hold.ClassId == admissionClass.Id &&
                hold.Status == HoldStatus.Confirmed
            )

            select new ClassResponseDto
            {
                Id = admissionClass.Id,
                Name = admissionClass.Name,
                TotalSeats = admissionClass.TotalSeats,

                HeldSeats = heldSeats,

                ConfirmedSeats = confirmedSeats,

                AvailableSeats =
                    admissionClass.TotalSeats -
                    heldSeats -
                    confirmedSeats
            };
    }
}
