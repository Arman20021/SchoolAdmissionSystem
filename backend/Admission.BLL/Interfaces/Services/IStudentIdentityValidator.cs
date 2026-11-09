
namespace Admission.BLL.Interfaces.Services;

public interface IStudentIdentityValidator
{
    bool IsValid(string? studentId);
}
