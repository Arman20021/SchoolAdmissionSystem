
using System.ComponentModel.DataAnnotations;

namespace Admission.BLL.DTOs.AdmissionHolds;

public class CreateAdmissionHoldDto
{
    [Required(ErrorMessage = "Class ID is required.")]
    [StringLength(40)]
    public string ClassId { get; set; } = string.Empty;
}
