namespace Anlasalamiyoruz.Application.Features.Debates.Commands.StartDebate;

public class StartDebateResponse
{
    public Guid DebateId { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// "P1" or "P2" — indicates which participant should answer this question.
    /// </summary>
    public string ForSide { get; set; } = string.Empty;
}
