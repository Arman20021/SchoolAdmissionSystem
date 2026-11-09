
using Admission.BLL.DTOs.AdmissionHolds;
using Admission.BLL.Entities;
using Admission.BLL.Enums;
using Admission.BLL.Interfaces.Repositories;
using Admission.BLL.Interfaces.Services;

namespace Admission.BLL.Services;

public class AdmissionHoldService : IAdmissionHoldService
{
    private readonly IAdmissionHoldRepository _repository;
    private readonly TimeSpan _holdDuration;

    public AdmissionHoldService(
        IAdmissionHoldRepository repository,
        TimeSpan holdDuration)
    {
        _repository = repository;

        if (holdDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(holdDuration),
                "Hold duration must be greater than zero."
            );
        }

        _holdDuration = holdDuration;
    }

    public async Task<(
        HoldCreationStatus Status,
        AdmissionHoldResponseDto? Hold
    )> CreateAsync(
        CreateAdmissionHoldDto request,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate Class ID
        string classId = request.ClassId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(classId) ||
            classId.Length > 40)
        {
            throw new ArgumentException(
                "A valid Class ID is required."
            );
        }

        // Validate Student ID
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits."
            );
        }

        // Generate Hold
        var now = DateTimeOffset.UtcNow;

        var admissionHold = new AdmissionHold
        {
            Id = $"h_{Guid.NewGuid():N}",
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(_holdDuration)
        };

        // Save through Repository
        var result = await _repository.TryCreateAsync(
            admissionHold,
            cancellationToken
        );

        // Handle unsuccessful creation
        if (result.Status != HoldCreationStatus.Created)
        {
            return (result.Status, null);
        }

        if (result.Hold is null)
        {
            throw new InvalidOperationException(
                "Repository returned Created without a Hold."
            );
        }

        // Map Entity to Response DTO
        var response = new AdmissionHoldResponseDto
        {
            Id = result.Hold.Id,
            ClassId = result.Hold.ClassId,
            Status = result.Hold.Status.ToString(),
            CreatedAtUtc = result.Hold.CreatedAtUtc,
            ExpiresAtUtc = result.Hold.ExpiresAtUtc
        };

        return (HoldCreationStatus.Created, response);
    }
}
