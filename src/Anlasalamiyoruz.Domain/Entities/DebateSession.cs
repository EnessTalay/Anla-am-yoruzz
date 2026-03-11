using Anlasalamiyoruz.Domain.Common;
using Anlasalamiyoruz.Domain.Enums;

namespace Anlasalamiyoruz.Domain.Entities;

public class DebateSession : BaseEntity
{
    public string Topic { get; private set; } = string.Empty;
    public DebateSessionStatus Status { get; private set; } = DebateSessionStatus.Pending;
    public string UserId { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<Participant> Participants { get; private set; } = new List<Participant>();
    public ICollection<ClarifyQuestion> ClarifyQuestions { get; private set; } = new List<ClarifyQuestion>();
    public ICollection<ActionStep> ActionSteps { get; private set; } = new List<ActionStep>();
    public ICollection<EmotionTag> EmotionTags { get; private set; } = new List<EmotionTag>();
    public AnalysisResult? AnalysisResult { get; private set; }

    private DebateSession() { }

    public static DebateSession Create(string topic, string userId)
    {
        return new DebateSession
        {
            Topic = topic,
            UserId = userId,
            Status = DebateSessionStatus.Pending
        };
    }

    public void UpdateStatus(DebateSessionStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
