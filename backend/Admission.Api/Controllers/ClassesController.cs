
using Admission.BLL.DTOs.Classes;
using Admission.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    // POST: /classes
    [HttpPost]
    public async Task<ActionResult<ClassResponseDto>> Create(
        [FromBody] CreateClassDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _classService.CreateAsync(
                request,
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetById),
                new { classId = result.Id },
                result
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid class information",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // GET: /classes
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetAllAsync(
            cancellationToken
        );

        return Ok(result);
    }

    // GET: /classes/{classId}
    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponseDto>> GetById(
        string classId,
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetByIdAsync(
            classId,
            cancellationToken
        );

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Class not found",
                Detail = $"No class exists with ID '{classId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result);
    }
}
