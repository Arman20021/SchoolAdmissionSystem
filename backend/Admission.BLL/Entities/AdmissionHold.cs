
using Admission.BLL.Entities;
using Admission.BLL.Enums;

namespace Admission.BLL.Entities;

public class AdmissionHold
{
    public string Id { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public HoldStatus Status { get; set; } = HoldStatus.Held;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    // Navigation Property
    public AdmissionClass? AdmissionClass { get; set; }
}
