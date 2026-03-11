namespace Anlasalamiyoruz.Application.Common.Models.AI;

public class DeepAnalysisResult
{
    public List<EmotionItem> Emotions { get; set; } = new();
    public ScoreItem Scores { get; set; } = new();
    public List<string> LeftPoints { get; set; } = new();
    public List<string> RightPoints { get; set; } = new();
    public List<string> BothPoints { get; set; } = new();
    public string Verdict { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public List<string> ActionSteps { get; set; } = new();
}

public class EmotionItem
{
    /// <summary>
    /// "left" or "right"
    /// </summary>
    public string Side { get; set; } = string.Empty;
    public string Emotion { get; set; } = string.Empty;

    /// <summary>
    /// Intensity on a scale of 1 (very low) to 5 (very high)
    /// </summary>
    public int Intensity { get; set; }
}

public class ScoreItem
{
    /// <summary>
    /// Left participant's validity score on a scale of 1-5
    /// </summary>
    public int LeftScore { get; set; }

    /// <summary>
    /// Right participant's validity score on a scale of 1-5
    /// </summary>
    public int RightScore { get; set; }

    public string Description { get; set; } = string.Empty;
}
