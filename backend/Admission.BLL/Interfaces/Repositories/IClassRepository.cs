
using Admission.BLL.DTOs.Classes;
using Admission.BLL.Entities;

namespace Admission.BLL.Interfaces.Repositories;

public interface IClassRepository
{
    Task AddAsync(
        AdmissionClass admissionClass,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ClassResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<ClassResponseDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    );
}
