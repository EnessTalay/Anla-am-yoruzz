using Anlasalamiyoruz.Application.Features.Debates.Commands.AnalyzeDebate;
using Anlasalamiyoruz.Application.Features.Debates.Commands.StartDebate;
using Anlasalamiyoruz.Application.Features.Debates.Commands.UpdateActionStep;
using Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Anlasalamiyoruz.API.Controllers;

[ApiController]
[Route("api/v1/debate")]
[Produces("application/json")]
public class DebateController : ControllerBase
{
    private readonly ISender _mediator;

    public DebateController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Starts a new debate session, generates AI clarifying questions and returns them.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartDebateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Start(
        [FromBody] StartDebateCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildValidationDetails(ex));
        }
    }

    /// <summary>
    /// Submits answers to clarifying questions and triggers the AI deep analysis.
    /// Returns the full analysis result (Venn data, scores, emotions, action steps).
    /// </summary>
    [HttpPost("{id:guid}/analyze")]
    [ProducesResponseType(typeof(AnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Analyze(
        [FromRoute] Guid id,
        [FromBody] AnalyzeDebateCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.DebateId)
            return BadRequest(new ProblemDetails
            {
                Title = "ID uyumsuzluğu",
                Detail = "URL'deki DebateId ile gövdedeki DebateId eşleşmiyor."
            });

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildValidationDetails(ex));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Geçersiz işlem", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Returns the persisted analysis result for the given debate session.
    /// </summary>
    [HttpGet("{id:guid}/result")]
    [ProducesResponseType(typeof(AnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResult(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetDebateResultQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Henüz analiz edilmedi", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Marks a specific action step as completed and persists the change.
    /// </summary>
    [HttpPatch("{id:guid}/steps/{stepId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteStep(
        [FromRoute] Guid id,
        [FromRoute] Guid stepId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new UpdateActionStepCommand { DebateId = id, StepId = stepId }, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Bulunamadı", Detail = ex.Message });
        }
    }

    private static ValidationProblemDetails BuildValidationDetails(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "Doğrulama hatası",
            Detail = "Lütfen formdaki hataları düzeltin."
        };
    }
}
