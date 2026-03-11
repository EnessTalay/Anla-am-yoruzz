using Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;
using MediatR;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.AnalyzeDebate;

public record AnalyzeDebateCommand : IRequest<AnalysisResultDto>
{
    public Guid DebateId { get; init; }
    public List<AnswerItem> Answers { get; init; } = new();
}

public class AnswerItem
{
    public Guid QuestionId { get; init; }
    public string AnswerText { get; init; } = string.Empty;
}
