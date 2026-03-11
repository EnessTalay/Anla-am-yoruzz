using Anlasalamiyoruz.Domain.Common;

namespace Anlasalamiyoruz.Domain.Entities;

public class AnalysisResult : BaseEntity
{
    /// <summary>
    /// JSON blob containing leftPoints, rightPoints and bothPoints for the Venn diagram.
    /// </summary>
    public string VennJson { get; private set; } = string.Empty;

    public string VerdictText { get; private set; } = string.Empty;
    public string SuggestionText { get; private set; } = string.Empty;

    public int LeftScore { get; private set; }
    public int RightScore { get; private set; }
    public string ScoreDescription { get; private set; } = string.Empty;

    public Guid SessionId { get; private set; }

    // Navigation property
    public DebateSession Session { get; private set; } = null!;

    private AnalysisResult() { }

    public static AnalysisResult Create(
        string vennJson,
        string verdictText,
        string suggestionText,
        int leftScore,
        int rightScore,
        string scoreDescription,
        Guid sessionId)
    {
        return new AnalysisResult
        {
            VennJson = vennJson,
            VerdictText = verdictText,
            SuggestionText = suggestionText,
            LeftScore = leftScore,
            RightScore = rightScore,
            ScoreDescription = scoreDescription,
            SessionId = sessionId
        };
    }
}
