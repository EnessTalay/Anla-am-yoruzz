using Anlasalamiyoruz.Domain.Common;

namespace Anlasalamiyoruz.Domain.Entities;

public class ActionStep : BaseEntity
{
    public string StepText { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; } = false;
    public DateTime? CompletedAt { get; private set; }
    public Guid SessionId { get; private set; }

    // Navigation property
    public DebateSession Session { get; private set; } = null!;

    private ActionStep() { }

    public static ActionStep Create(string stepText, Guid sessionId)
    {
        return new ActionStep
        {
            StepText = stepText,
            SessionId = sessionId
        };
    }

    public void MarkAsCompleted()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
