using System.Text.Json;
using Anlasalamiyoruz.Application.Common.Interfaces;
using Anlasalamiyoruz.Application.Common.Models.AI;
using Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;
using Anlasalamiyoruz.Domain.Entities;
using Anlasalamiyoruz.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.AnalyzeDebate;

public class AnalyzeDebateCommandHandler : IRequestHandler<AnalyzeDebateCommand, AnalysisResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiAnalysisService _aiService;
    private readonly ILogger<AnalyzeDebateCommandHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AnalyzeDebateCommandHandler(
        IApplicationDbContext context,
        IAiAnalysisService aiService,
        ILogger<AnalyzeDebateCommandHandler> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<AnalysisResultDto> Handle(
        AnalyzeDebateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting deep analysis for DebateSession {SessionId}.", request.DebateId);

        // ── Step 1: Load session with all related data ────────────────────────
        var session = await _context.DebateSessions
            .Include(s => s.Participants)
            .Include(s => s.ClarifyQuestions)
            .FirstOrDefaultAsync(s => s.Id == request.DebateId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"'{request.DebateId}' ID'li tartışma oturumu bulunamadı.");

        if (session.Status == DebateSessionStatus.Analyzed)
            throw new InvalidOperationException(
                "Bu oturum zaten analiz edilmiş. Sonuçları görmek için GET /result endpoint'ini kullanın.");

        // ── Step 2: Apply submitted answers to ClarifyQuestion records ────────
        var questionLookup = session.ClarifyQuestions.ToDictionary(q => q.Id);

        foreach (var answer in request.Answers)
        {
            if (questionLookup.TryGetValue(answer.QuestionId, out var question))
            {
                question.SetAnswer(answer.AnswerText);
                _logger.LogDebug(
                    "Answer set for question {QuestionId}.", answer.QuestionId);
            }
            else
            {
                _logger.LogWarning(
                    "QuestionId {QuestionId} not found in session {SessionId}. Skipping.",
                    answer.QuestionId, request.DebateId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // ── Step 3: Build DeepAnalysisRequest ─────────────────────────────────
        var p1 = session.Participants.FirstOrDefault(p => p.Side == ParticipantSide.P1)
            ?? throw new InvalidOperationException("P1 katılımcısı bulunamadı.");
        var p2 = session.Participants.FirstOrDefault(p => p.Side == ParticipantSide.P2)
            ?? throw new InvalidOperationException("P2 katılımcısı bulunamadı.");

        var leftAnswers = session.ClarifyQuestions
            .Where(q => q.ForSide == ParticipantSide.P1 && q.Answer != null)
            .Select(q => new ParticipantAnswerItem
            {
                Question = q.QuestionText,
                Answer = q.Answer!
            })
            .ToList();

        var rightAnswers = session.ClarifyQuestions
            .Where(q => q.ForSide == ParticipantSide.P2 && q.Answer != null)
            .Select(q => new ParticipantAnswerItem
            {
                Question = q.QuestionText,
                Answer = q.Answer!
            })
            .ToList();

        var aiRequest = new DeepAnalysisRequest
        {
            Topic = session.Topic,
            LeftViewText = p1.ViewText,
            RightViewText = p2.ViewText,
            LeftAnswers = leftAnswers,
            RightAnswers = rightAnswers
        };

        // ── Step 4: Call Gemini deep analysis ─────────────────────────────────
        _logger.LogInformation(
            "Sending DeepAnalysisRequest to AI service for session {SessionId}.", request.DebateId);

        var aiResult = await _aiService.PerformDeepAnalysisAsync(aiRequest, cancellationToken);

        _logger.LogInformation(
            "AI analysis complete. LeftScore: {L}/5, RightScore: {R}/5.",
            aiResult.Scores.LeftScore, aiResult.Scores.RightScore);

        // ── Step 5: Persist results ───────────────────────────────────────────

        // 5a. Serialize Venn data as JSON blob
        var vennPayload = new
        {
            leftPoints = aiResult.LeftPoints,
            rightPoints = aiResult.RightPoints,
            bothPoints = aiResult.BothPoints
        };
        var vennJson = JsonSerializer.Serialize(vennPayload, JsonOptions);

        // 5b. AnalysisResult record
        var analysisResult = AnalysisResult.Create(
            vennJson: vennJson,
            verdictText: aiResult.Verdict,
            suggestionText: aiResult.Suggestion,
            leftScore: aiResult.Scores.LeftScore,
            rightScore: aiResult.Scores.RightScore,
            scoreDescription: aiResult.Scores.Description,
            sessionId: session.Id);

        await _context.AnalysisResults.AddAsync(analysisResult, cancellationToken);

        // 5c. ActionStep records — collect locally so BuildDto can use them immediately
        var createdSteps = new List<ActionStep>();
        foreach (var stepText in aiResult.ActionSteps)
        {
            var step = ActionStep.Create(stepText, session.Id);
            await _context.ActionSteps.AddAsync(step, cancellationToken);
            createdSteps.Add(step);
        }

        // 5d. EmotionTag records — collect locally so BuildDto can use them immediately
        var createdTags = new List<EmotionTag>();
        foreach (var emotion in aiResult.Emotions)
        {
            var side = emotion.Side.Trim().ToLowerInvariant() == "left"
                ? ParticipantSide.P1
                : ParticipantSide.P2;

            var tag = EmotionTag.Create(side, emotion.Emotion, emotion.Intensity, session.Id);
            await _context.EmotionTags.AddAsync(tag, cancellationToken);
            createdTags.Add(tag);
        }

        // 5e. Update session status
        session.UpdateStatus(DebateSessionStatus.Analyzed);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "DebateSession {SessionId} marked as Analyzed. Analysis persisted.", session.Id);

        // ── Step 6: Build and return response DTO ────────────────────────────
        return BuildDto(session.Id, aiResult, createdSteps, createdTags);
    }

    private static AnalysisResultDto BuildDto(
        Guid debateId,
        Application.Common.Models.AI.DeepAnalysisResult aiResult,
        IEnumerable<ActionStep> actionSteps,
        IEnumerable<EmotionTag> emotionTags)
    {
        return new AnalysisResultDto
        {
            DebateId = debateId,
            Status = DebateSessionStatus.Analyzed.ToString(),
            Venn = new VennDto
            {
                LeftPoints = aiResult.LeftPoints,
                RightPoints = aiResult.RightPoints,
                BothPoints = aiResult.BothPoints
            },
            Scores = new ScoreDto
            {
                LeftScore = aiResult.Scores.LeftScore,
                RightScore = aiResult.Scores.RightScore,
                Description = aiResult.Scores.Description
            },
            Emotions = aiResult.Emotions.Select(e => new EmotionDto
            {
                Side = e.Side,
                Emotion = e.Emotion,
                Intensity = e.Intensity
            }).ToList(),
            Verdict = aiResult.Verdict,
            Suggestion = aiResult.Suggestion,
            ActionSteps = actionSteps.Select(s => new ActionStepDto
            {
                Id = s.Id,
                StepText = s.StepText,
                IsCompleted = s.IsCompleted
            }).ToList()
        };
    }
}
