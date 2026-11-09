
using Admission.Api.Filters;
using Admission.BLL.DTOs.AdmissionHolds;
using Admission.BLL.Enums;
using Admission.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("admission-holds")]
public class AdmissionHoldsController : ControllerBase
{
    private readonly IAdmissionHoldService _holdService;

    public AdmissionHoldsController(
        IAdmissionHoldService holdService)
    {
        _holdService = holdService;
    }

    // POST: /admission-holds
    [HttpPost]
    [ServiceFilter(typeof(StudentIdentityFilter))]
    public async Task<ActionResult<AdmissionHoldResponseDto>> Create(
        [FromBody] CreateAdmissionHoldDto request,
        [FromHeader(Name = "X-Student-Id")] string studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _holdService.CreateAsync(
                request,
                studentId,
                cancellationToken
            );

            if (result.Status == HoldCreationStatus.Created)
            {
                if (result.Hold == null)
                {
                    return Problem(
                        title: "Hold creation failed",
                        detail: "The repository returned an empty hold.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                return StatusCode(
                    StatusCodes.Status201Created,
                    result.Hold
                );
            }

            if (result.Status == HoldCreationStatus.ClassNotFound)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Class not found",
                    Detail = "The requested class does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (result.Status == HoldCreationStatus.NoSeatsAvailable)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "No seats available",
                    Detail = "All seats in this class are occupied.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            if (result.Status == HoldCreationStatus.DuplicateApplication)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Duplicate application",
                    Detail = "You already have an active application for this class.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return Problem(
                title: "Unexpected hold creation result",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
