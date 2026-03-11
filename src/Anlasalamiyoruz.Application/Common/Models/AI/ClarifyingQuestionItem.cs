namespace Anlasalamiyoruz.Application.Common.Models.AI;

public class ClarifyingQuestionItem
{
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// "Left", "Right" or "Both"
    /// </summary>
    public string TargetSide { get; set; } = string.Empty;
}
