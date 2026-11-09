
using Admission.BLL.DTOs.Classes;
using Admission.BLL.Entities;
using Admission.BLL.Interfaces.Repositories;
using Admission.BLL.Interfaces.Services;

namespace Admission.BLL.Services;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;

    public ClassService(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<ClassResponseDto> CreateAsync(
        CreateClassDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Class name is required."
            );
        }

        if (name.Length > 150)
        {
            throw new ArgumentException(
                "Class name cannot exceed 150 characters."
            );
        }

        if (request.TotalSeats <= 0)
        {
            throw new ArgumentException(
                "Total seats must be greater than zero."
            );
        }

        var admissionClass = new AdmissionClass
        {
            Id = $"c_{Guid.NewGuid():N}",
            Name = name,
            TotalSeats = request.TotalSeats,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await _classRepository.AddAsync(
            admissionClass,
            cancellationToken
        );

        return new ClassResponseDto
        {
            Id = admissionClass.Id,
            Name = admissionClass.Name,
            TotalSeats = admissionClass.TotalSeats,
            HeldSeats = 0,
            ConfirmedSeats = 0,
            AvailableSeats = admissionClass.TotalSeats
        };
    }

    public async Task<IReadOnlyList<ClassResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _classRepository.GetAllAsync(
            cancellationToken
        );
    }

    public async Task<ClassResponseDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Class ID is required."
            );
        }

        return await _classRepository.GetByIdAsync(
            id,
            cancellationToken
        );
    }
}
