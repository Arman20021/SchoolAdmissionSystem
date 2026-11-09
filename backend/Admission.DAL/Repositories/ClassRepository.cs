
using Admission.BLL.Entities;
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

    public async Task<IReadOnlyList<AdmissionClass>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.AdmissionClasses
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdmissionClass?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _context.AdmissionClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );
    }
}
