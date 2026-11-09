using Admission.BLL.DTOs.Classes;
using Admission.BLL.Entities;
using Admission.BLL.Interfaces.Repositories;
using Admission.BLL.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        return MapToResponse(admissionClass);
    }

    public async Task<IReadOnlyList<ClassResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var classes = await _classRepository.GetAllAsync(
            cancellationToken
        );

        return classes
            .Select(MapToResponse)
            .ToList();
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

        var admissionClass = await _classRepository.GetByIdAsync(
            id,
            cancellationToken
        );

        return admissionClass == null
            ? null
            : MapToResponse(admissionClass);
    }

    private static ClassResponseDto MapToResponse(
        AdmissionClass admissionClass)
    {
        return new ClassResponseDto
        {
            Id = admissionClass.Id,
            Name = admissionClass.Name,
            TotalSeats = admissionClass.TotalSeats,

            // Hold feature is not implemented yet.
            HeldSeats = 0,
            ConfirmedSeats = 0,
            AvailableSeats = admissionClass.TotalSeats
        };
    }
}