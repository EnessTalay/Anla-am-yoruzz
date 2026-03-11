using MediatR;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.StartDebate;

public record StartDebateCommand : IRequest<StartDebateResponse>
{
    public string Topic { get; init; } = string.Empty;
    public string Person1Name { get; init; } = string.Empty;
    public string Person1View { get; init; } = string.Empty;
    public string Person2Name { get; init; } = string.Empty;
    public string Person2View { get; init; } = string.Empty;
}
