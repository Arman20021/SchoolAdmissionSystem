
using System.Text.RegularExpressions;
using Admission.BLL.Interfaces.Services;

namespace Admission.BLL.Services;

public class StudentIdentityValidator : IStudentIdentityValidator
{
    private static readonly Regex StudentIdRegex =
        new(@"\A[0-9]{17}\z", RegexOptions.Compiled);

    public bool IsValid(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return false;
        }

        return StudentIdRegex.IsMatch(studentId);
    }
}
