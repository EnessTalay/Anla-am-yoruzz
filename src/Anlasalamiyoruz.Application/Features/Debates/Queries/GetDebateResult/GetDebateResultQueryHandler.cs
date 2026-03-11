using System.Text.Json;
using Anlasalamiyoruz.Application.Common.Interfaces;
using Anlasalamiyoruz.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;

public class GetDebateResultQueryHandler : IRequestHandler<GetDebateResultQuery, AnalysisResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDebateResultQueryHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetDebateResultQueryHandler(
        IApplicationDbContext context,
        ILogger<GetDebateResultQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalysisResultDto> Handle(
        GetDebateResultQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching analysis result for DebateSession {SessionId}.", request.DebateId);

        var session = await _context.DebateSessions
            .Include(s => s.AnalysisResult)
            .Include(s => s.ActionSteps)
            .Include(s => s.EmotionTags)
            .FirstOrDefaultAsync(s => s.Id == request.DebateId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"'{request.DebateId}' ID'li tartışma oturumu bulunamadı.");

        if (session.AnalysisResult is null)
            throw new InvalidOperationException(
                "Bu oturum henüz analiz edilmemiş. Önce POST /analyze endpoint'ini çağırın.");

        // Deserialize Venn JSON blob back to typed object
        var venn = JsonSerializer.Deserialize<VennPayload>(
            session.AnalysisResult.VennJson, JsonOptions)
            ?? new VennPayload();

        var dto = new AnalysisResultDto
        {
            DebateId = session.Id,
            Status = session.Status.ToString(),
            Venn = new VennDto
            {
                LeftPoints = venn.LeftPoints,
                RightPoints = venn.RightPoints,
                BothPoints = venn.BothPoints
            },
            Scores = new ScoreDto
            {
                LeftScore = session.AnalysisResult.LeftScore,
                RightScore = session.AnalysisResult.RightScore,
                Description = session.AnalysisResult.ScoreDescription
            },
            Emotions = session.EmotionTags.Select(e => new EmotionDto
            {
                Side = e.Side == ParticipantSide.P1 ? "left" : "right",
                Emotion = e.EmotionKey,
                Intensity = e.Intensity
            }).ToList(),
            Verdict = session.AnalysisResult.VerdictText,
            Suggestion = session.AnalysisResult.SuggestionText,
            ActionSteps = session.ActionSteps
                .OrderBy(s => s.CreatedAt)
                .Select(s => new ActionStepDto
                {
                    Id = s.Id,
                    StepText = s.StepText,
                    IsCompleted = s.IsCompleted
                }).ToList()
        };

        _logger.LogInformation(
            "Analysis result returned for session {SessionId}.", request.DebateId);

        return dto;
    }

    /// <summary>
    /// Internal model used only for deserializing the stored VennJson blob.
    /// </summary>
    private sealed class VennPayload
    {
        public List<string> LeftPoints { get; set; } = new();
        public List<string> RightPoints { get; set; } = new();
        public List<string> BothPoints { get; set; } = new();
    }
}
