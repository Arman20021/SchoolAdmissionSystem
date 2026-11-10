
using Admission.BLL.DTOs.AdmissionHolds;
using Admission.BLL.Enums;

namespace Admission.BLL.Interfaces.Services;

public interface IAdmissionHoldService
{
    Task<(HoldCreationStatus Status, AdmissionHoldResponseDto? Hold)>
        CreateAsync(
            CreateAdmissionHoldDto request,
            string studentId,
            CancellationToken cancellationToken = default
        );

    Task<(HoldConfirmationStatus Status, AdmissionHoldResponseDto? Hold)>
        ConfirmAsync(
            string holdId,
            string studentId,
            CancellationToken cancellationToken = default
        );

    Task<(HoldLookupStatus Status, AdmissionHoldResponseDto? Hold)>
        GetByIdAsync(
            string holdId,
            string studentId,
            CancellationToken cancellationToken = default
        );

    Task<(HoldCancellationStatus Status, AdmissionHoldResponseDto? Hold)>
        CancelAsync(
            string holdId,
            string studentId,
            CancellationToken cancellationToken = default
        );
}
