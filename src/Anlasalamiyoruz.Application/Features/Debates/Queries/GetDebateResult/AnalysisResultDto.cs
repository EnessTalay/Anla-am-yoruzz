namespace Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;

public class AnalysisResultDto
{
    public Guid DebateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public VennDto Venn { get; set; } = new();
    public ScoreDto Scores { get; set; } = new();
    public List<EmotionDto> Emotions { get; set; } = new();
    public string Verdict { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public List<ActionStepDto> ActionSteps { get; set; } = new();
}

public class VennDto
{
    public List<string> LeftPoints { get; set; } = new();
    public List<string> RightPoints { get; set; } = new();
    public List<string> BothPoints { get; set; } = new();
}

public class ScoreDto
{
    public int LeftScore { get; set; }
    public int RightScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class EmotionDto
{
    public string Side { get; set; } = string.Empty;
    public string Emotion { get; set; } = string.Empty;
    public int Intensity { get; set; }
}

public class ActionStepDto
{
    public Guid Id { get; set; }
    public string StepText { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
