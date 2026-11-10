
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

    public async Task<(
        HoldConfirmationStatus Status,
        AdmissionHoldResponseDto? Hold
    )> ConfirmAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate Hold ID
        holdId = holdId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(holdId) ||
            holdId.Length > 40)
        {
            throw new ArgumentException(
                "A valid Hold ID is required."
            );
        }

        // 2. Validate Student ID
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits."
            );
        }

      
        var currentUtcTime = DateTimeOffset.UtcNow;

     
        var result = await _repository.TryConfirmAsync(
            holdId,
            studentId,
            currentUtcTime,
            cancellationToken
        );

 
        if (result.Hold is null)
        {
            return (result.Status, null);
        }

         
        var response = new AdmissionHoldResponseDto
        {
            Id = result.Hold.Id,
            ClassId = result.Hold.ClassId,
            Status = result.Hold.Status.ToString(),
            CreatedAtUtc = result.Hold.CreatedAtUtc,
            ExpiresAtUtc = result.Hold.ExpiresAtUtc
        };

       
        return (result.Status, response);
    }

    public async Task<(
        HoldLookupStatus Status,
        AdmissionHoldResponseDto? Hold
    )> GetByIdAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate Hold ID
        holdId = holdId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(holdId) ||
            holdId.Length > 40)
        {
            throw new ArgumentException(
                "A valid Hold ID is required."
            );
        }

        // 2. Validate Student ID
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits."
            );
        }

        // 3. Get current UTC time
        var currentUtcTime = DateTimeOffset.UtcNow;

        // 4. Get hold through Repository
        var result = await _repository.TryGetByIdAsync(
            holdId,
            studentId,
            currentUtcTime,
            cancellationToken
        );

        // 5. Handle unsuccessful lookup
        if (result.Status != HoldLookupStatus.Found)
        {
            return (result.Status, null);
        }

        if (result.Hold is null)
        {
            throw new InvalidOperationException(
                "Repository returned Found without a Hold."
            );
        }

        // 6. Map Entity to Response DTO
        var response = new AdmissionHoldResponseDto
        {
            Id = result.Hold.Id,
            ClassId = result.Hold.ClassId,
            Status = result.Hold.Status.ToString(),
            CreatedAtUtc = result.Hold.CreatedAtUtc,
            ExpiresAtUtc = result.Hold.ExpiresAtUtc
        };

        return (HoldLookupStatus.Found, response);
    }




    public async Task<(
        HoldCancellationStatus Status,
        AdmissionHoldResponseDto? Hold
    )> CancelAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate Hold ID
        holdId = holdId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(holdId) ||
            holdId.Length > 40)
        {
            throw new ArgumentException(
                "A valid Hold ID is required."
            );
        }

        // 2. Validate Student ID
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits."
            );
        }

        // 3. Get current UTC time
        var currentUtcTime = DateTimeOffset.UtcNow;

        // 4. Cancel hold through Repository
        var result = await _repository.TryCancelAsync(
            holdId,
            studentId,
            currentUtcTime,
            cancellationToken
        );

        // 5. Handle unsuccessful cancellation
        if (result.Hold is null)
        {
            return (result.Status, null);
        }

        // 6. Map Entity to Response DTO
        var response = new AdmissionHoldResponseDto
        {
            Id = result.Hold.Id,
            ClassId = result.Hold.ClassId,
            Status = result.Hold.Status.ToString(),
            CreatedAtUtc = result.Hold.CreatedAtUtc,
            ExpiresAtUtc = result.Hold.ExpiresAtUtc
        };

        return (result.Status, response);
    }



    public async Task<IReadOnlyList<AdmissionHoldResponseDto>>
        GetByStudentIdAsync(
            string studentId,
            CancellationToken cancellationToken = default)
    {
        // 1. Validate Student ID
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            studentId.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "Student ID must contain exactly 17 digits."
            );
        }

        // 2. Get current UTC time
        var currentUtcTime = DateTimeOffset.UtcNow;

        // 3. Retrieve all student holds through Repository
        var holds = await _repository.GetByStudentIdAsync(
            studentId,
            currentUtcTime,
            cancellationToken
        );

        // 4. Map Entities to Response DTOs
        var response = holds
            .Select(hold => new AdmissionHoldResponseDto
            {
                Id = hold.Id,
                ClassId = hold.ClassId,
                Status = hold.Status.ToString(),
                CreatedAtUtc = hold.CreatedAtUtc,
                ExpiresAtUtc = hold.ExpiresAtUtc
            })
            .ToList();

        // 5. Return all student applications
        return response;
    }

}
