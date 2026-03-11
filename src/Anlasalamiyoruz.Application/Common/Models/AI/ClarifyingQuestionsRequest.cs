namespace Anlasalamiyoruz.Application.Common.Models.AI;

public class ClarifyingQuestionsRequest
{
    public string Topic { get; set; } = string.Empty;
    public string LeftViewText { get; set; } = string.Empty;
    public string RightViewText { get; set; } = string.Empty;
}
