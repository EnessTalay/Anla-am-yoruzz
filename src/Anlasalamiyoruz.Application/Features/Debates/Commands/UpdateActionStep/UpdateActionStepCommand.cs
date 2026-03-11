using MediatR;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.UpdateActionStep;

public record UpdateActionStepCommand : IRequest
{
    public Guid DebateId { get; init; }
    public Guid StepId { get; init; }
}
