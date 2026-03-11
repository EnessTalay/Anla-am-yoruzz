using Anlasalamiyoruz.Domain.Common;
using Anlasalamiyoruz.Domain.Enums;

namespace Anlasalamiyoruz.Domain.Entities;

public class ClarifyQuestion : BaseEntity
{
    public ParticipantSide ForSide { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public string? Answer { get; private set; }
    public Guid SessionId { get; private set; }

    // Navigation property
    public DebateSession Session { get; private set; } = null!;

    private ClarifyQuestion() { }

    public static ClarifyQuestion Create(ParticipantSide forSide, string questionText, Guid sessionId)
    {
        return new ClarifyQuestion
        {
            ForSide = forSide,
            QuestionText = questionText,
            SessionId = sessionId
        };
    }

    public void SetAnswer(string answer)
    {
        Answer = answer;
        UpdatedAt = DateTime.UtcNow;
    }
}
