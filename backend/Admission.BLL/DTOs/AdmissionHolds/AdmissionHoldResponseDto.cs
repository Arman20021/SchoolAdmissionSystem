
//namespace Admission.BLL.DTOs.AdmissionHolds;

//public class AdmissionHoldResponseDto
//{
//    public string Id { get; set; } = string.Empty;

//    public string ClassId { get; set; } = string.Empty;

//    public string Status { get; set; } = string.Empty;

//    public DateTimeOffset CreatedAtUtc { get; set; }

//    public DateTimeOffset ExpiresAtUtc { get; set; }
//}





using System.Text.Json.Serialization;

namespace Admission.BLL.DTOs.AdmissionHolds;

public class AdmissionHoldResponseDto
{
    public string HoldId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StudentId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ExpiresAt { get; set; }
}
