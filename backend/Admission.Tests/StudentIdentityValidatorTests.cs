
using Admission.BLL.Services;

namespace Admission.Tests;

public class StudentIdentityValidatorTests
{
    private readonly StudentIdentityValidator _validator = new();

    [Theory]
    [InlineData("20152691234567890", true)]
    [InlineData("1234567890123456", false)]
    [InlineData("123456789012345678", false)]
    [InlineData("12345678901234abc", false)]
    [InlineData("", false)]
    [InlineData("                 ", false)]
    [InlineData(null, false)]
    [InlineData(" 20152691234567890", false)]
    public void IsValid_ShouldReturnExpectedResult(
        string? studentId,
        bool expected)
    {
        // Act
        var result = _validator.IsValid(studentId);

        // Assert
        Assert.Equal(expected, result);
    }
}
