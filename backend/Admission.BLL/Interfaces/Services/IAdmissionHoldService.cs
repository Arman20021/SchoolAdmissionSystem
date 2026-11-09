
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
}
