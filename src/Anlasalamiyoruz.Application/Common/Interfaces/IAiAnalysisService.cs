using Anlasalamiyoruz.Application.Common.Models.AI;

namespace Anlasalamiyoruz.Application.Common.Interfaces;

public interface IAiAnalysisService
{
    /// <summary>
    /// Generates 2-3 clarifying questions for both sides to deepen context.
    /// </summary>
    Task<List<ClarifyingQuestionItem>> GenerateClarifyingQuestionsAsync(
        ClarifyingQuestionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full mediator-style deep analysis combining all views and answers.
    /// Returns emotions, scores, point lists, verdict, suggestion and action steps.
    /// </summary>
    Task<DeepAnalysisResult> PerformDeepAnalysisAsync(
        DeepAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
