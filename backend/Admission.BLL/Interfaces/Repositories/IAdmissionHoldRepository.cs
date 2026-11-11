
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



    Task<(HoldConfirmationStatus Status, AdmissionHold? Hold)>
        TryConfirmAsync(
            string holdId,
            string studentId,
            DateTimeOffset currentUtcTime,
            CancellationToken cancellationToken = default
        );



    Task<(HoldLookupStatus Status, AdmissionHold? Hold)>
        TryGetByIdAsync(
            string holdId,
            string studentId,
            DateTimeOffset currentUtcTime,
            CancellationToken cancellationToken = default
        );




    Task<(HoldCancellationStatus Status, AdmissionHold? Hold)>
        TryCancelAsync(
            string holdId,
            string studentId,
            DateTimeOffset currentUtcTime,
            CancellationToken cancellationToken = default
        );



    Task<IReadOnlyList<(AdmissionHold Hold, string ClassName)>>
        GetByStudentIdAsync(
            string studentId,
            DateTimeOffset currentUtcTime,
            CancellationToken cancellationToken = default
        );

}
