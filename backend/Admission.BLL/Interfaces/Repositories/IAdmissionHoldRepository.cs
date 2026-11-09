
using Admission.BLL.Entities;
using Admission.BLL.Enums;

namespace Admission.BLL.Interfaces.Repositories;

public interface IAdmissionHoldRepository
{
    Task<(HoldCreationStatus Status, AdmissionHold? Hold)>
        TryCreateAsync(
            AdmissionHold hold,
            CancellationToken cancellationToken = default
        );


    Task<int> ExpireHoldsAsync(
        DateTimeOffset currentUtcTime,
        CancellationToken cancellationToken = default
    );

}
