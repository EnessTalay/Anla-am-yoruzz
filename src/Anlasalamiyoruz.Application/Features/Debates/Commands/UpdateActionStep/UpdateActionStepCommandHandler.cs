using Anlasalamiyoruz.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.UpdateActionStep;

public class UpdateActionStepCommandHandler : IRequestHandler<UpdateActionStepCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateActionStepCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateActionStepCommand request, CancellationToken cancellationToken)
    {
        var step = await _context.ActionSteps
            .FirstOrDefaultAsync(s => s.Id == request.StepId && s.SessionId == request.DebateId, cancellationToken)
            ?? throw new KeyNotFoundException($"Eylem adımı bulunamadı. StepId: {request.StepId}");

        if (!step.IsCompleted)
            step.MarkAsCompleted();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
