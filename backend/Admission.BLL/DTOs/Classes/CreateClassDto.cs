
using System.ComponentModel.DataAnnotations;

namespace Admission.BLL.DTOs.Classes;

public class CreateClassDto
{
    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(
        150,
        MinimumLength = 1,
        ErrorMessage = "Class name must be between 1 and 150 characters."
    )]
    public string Name { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Total seats must be greater than zero."
    )]
    public int TotalSeats { get; set; }
}
