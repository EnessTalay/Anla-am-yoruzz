using Anlasalamiyoruz.Domain.Common;
using Anlasalamiyoruz.Domain.Enums;

namespace Anlasalamiyoruz.Domain.Entities;

public class EmotionTag : BaseEntity
{
    public ParticipantSide Side { get; private set; }
    public string EmotionKey { get; private set; } = string.Empty;

    /// <summary>
    /// Intensity on a scale of 1 (very low) to 5 (very high), as reported by the AI.
    /// </summary>
    public int Intensity { get; private set; }

    public Guid SessionId { get; private set; }

    // Navigation property
    public DebateSession Session { get; private set; } = null!;

    private EmotionTag() { }

    public static EmotionTag Create(ParticipantSide side, string emotionKey, int intensity, Guid sessionId)
    {
        return new EmotionTag
        {
            Side = side,
            EmotionKey = emotionKey,
            Intensity = intensity,
            SessionId = sessionId
        };
    }
}
