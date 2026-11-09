
using Admission.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Admission.Api.Filters;

public class StudentIdentityFilter : IAsyncActionFilter
{
    public const string StudentIdItemKey = "StudentId";

    private const string StudentIdHeader = "X-Student-Id";

    private readonly IStudentIdentityValidator _validator;

    public StudentIdentityFilter(
        IStudentIdentityValidator validator)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var studentId = context.HttpContext
            .Request.Headers[StudentIdHeader]
            .ToString();

        if (!_validator.IsValid(studentId))
        {
            var problem = new ProblemDetails
            {
                Title = "Invalid Student Identity",
                Detail = string.IsNullOrWhiteSpace(studentId)
                    ? "X-Student-Id header is required."
                    : "Student ID must contain exactly 17 digits.",
                Status = StatusCodes.Status400BadRequest
            };

            context.Result = new BadRequestObjectResult(problem);

            return;
        }

        context.HttpContext.Items[StudentIdItemKey] = studentId;

        await next();
    }
}
