using Anlasalamiyoruz.Domain.Common;
using Anlasalamiyoruz.Domain.Enums;

namespace Anlasalamiyoruz.Domain.Entities;

public class Participant : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string ViewText { get; private set; } = string.Empty;
    public ParticipantSide Side { get; private set; }
    public Guid SessionId { get; private set; }

    // Navigation property
    public DebateSession Session { get; private set; } = null!;

    private Participant() { }

    public static Participant Create(string name, string viewText, ParticipantSide side, Guid sessionId)
    {
        return new Participant
        {
            Name = name,
            ViewText = viewText,
            Side = side,
            SessionId = sessionId
        };
    }
}
