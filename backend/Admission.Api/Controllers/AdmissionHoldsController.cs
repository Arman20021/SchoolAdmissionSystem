
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

    // POST: /holds/{holdId}/confirm
    [HttpPost("/holds/{holdId}/confirm")]
    [ServiceFilter(typeof(StudentIdentityFilter))]
    public async Task<ActionResult<AdmissionHoldResponseDto>> Confirm(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _holdService.ConfirmAsync(
                holdId,
                studentId,
                cancellationToken
            );

            switch (result.Status)
            {
                case HoldConfirmationStatus.Confirmed:

                    if (result.Hold == null)
                    {
                        return Problem(
                            title: "Confirmation failed",
                            detail: "The repository returned an empty hold.",
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }

                    return Ok(result.Hold);

                case HoldConfirmationStatus.NotFound:

                    return NotFound(new ProblemDetails
                    {
                        Title = "Hold not found",
                        Detail = "The requested hold does not exist.",
                        Status = StatusCodes.Status404NotFound
                    });

                case HoldConfirmationStatus.Forbidden:

                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ProblemDetails
                        {
                            Title = "Access denied",
                            Detail = "You cannot confirm another student's hold.",
                            Status = StatusCodes.Status403Forbidden
                        }
                    );

                case HoldConfirmationStatus.AlreadyConfirmed:

                    return Conflict(new ProblemDetails
                    {
                        Title = "Already confirmed",
                        Detail = "This admission hold has already been confirmed.",
                        Status = StatusCodes.Status409Conflict
                    });

                case HoldConfirmationStatus.Expired:

                    return Conflict(new ProblemDetails
                    {
                        Title = "Hold expired",
                        Detail = "This admission hold has expired.",
                        Status = StatusCodes.Status409Conflict
                    });

                case HoldConfirmationStatus.Cancelled:

                    return Conflict(new ProblemDetails
                    {
                        Title = "Hold cancelled",
                        Detail = "This admission hold has been cancelled.",
                        Status = StatusCodes.Status409Conflict
                    });

                default:

                    return Problem(
                        title: "Unexpected confirmation result",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
            }
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




    // GET: /holds/{holdId}
    [HttpGet("/holds/{holdId}")]
    [ServiceFilter(typeof(StudentIdentityFilter))]
    public async Task<ActionResult<AdmissionHoldResponseDto>> GetById(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _holdService.GetByIdAsync(
                holdId,
                studentId,
                cancellationToken
            );

            switch (result.Status)
            {
                case HoldLookupStatus.Found:

                    if (result.Hold == null)
                    {
                        return Problem(
                            title: "Hold lookup failed",
                            detail: "The repository returned an empty hold.",
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }

                    return Ok(result.Hold);

                case HoldLookupStatus.NotFound:

                    return NotFound(new ProblemDetails
                    {
                        Title = "Hold not found",
                        Detail = "The requested admission hold does not exist.",
                        Status = StatusCodes.Status404NotFound
                    });

                case HoldLookupStatus.Forbidden:

                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ProblemDetails
                        {
                            Title = "Access denied",
                            Detail = "You cannot access another student's hold.",
                            Status = StatusCodes.Status403Forbidden
                        }
                    );

                default:

                    return Problem(
                        title: "Unexpected hold lookup result",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
            }
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
