namespace Anlasalamiyoruz.Application.Common.Models.AI;

public class DeepAnalysisRequest
{
    public string Topic { get; set; } = string.Empty;
    public string LeftViewText { get; set; } = string.Empty;
    public string RightViewText { get; set; } = string.Empty;
    public List<ParticipantAnswerItem> LeftAnswers { get; set; } = new();
    public List<ParticipantAnswerItem> RightAnswers { get; set; } = new();
}

public class ParticipantAnswerItem
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
