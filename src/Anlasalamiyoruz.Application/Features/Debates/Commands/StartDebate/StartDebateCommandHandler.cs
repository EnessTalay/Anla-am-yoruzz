using Anlasalamiyoruz.Application.Common.Interfaces;
using Anlasalamiyoruz.Application.Common.Models.AI;
using Anlasalamiyoruz.Domain.Entities;
using Anlasalamiyoruz.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.StartDebate;

public class StartDebateCommandHandler : IRequestHandler<StartDebateCommand, StartDebateResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiAnalysisService _aiService;
    private readonly ILogger<StartDebateCommandHandler> _logger;

    public StartDebateCommandHandler(
        IApplicationDbContext context,
        IAiAnalysisService aiService,
        ILogger<StartDebateCommandHandler> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<StartDebateResponse> Handle(
        StartDebateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting new debate session. Topic: {Topic}", request.Topic);

        // ── Step 1: Create and persist the DebateSession ─────────────────────
        var session = DebateSession.Create(request.Topic, userId: "anonymous");
        await _context.DebateSessions.AddAsync(session, cancellationToken);

        // ── Step 2: Create and persist both Participants ──────────────────────
        var participant1 = Participant.Create(
            request.Person1Name,
            request.Person1View,
            ParticipantSide.P1,
            session.Id);

        var participant2 = Participant.Create(
            request.Person2Name,
            request.Person2View,
            ParticipantSide.P2,
            session.Id);

        await _context.Participants.AddAsync(participant1, cancellationToken);
        await _context.Participants.AddAsync(participant2, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "DebateSession {SessionId} and participants saved to database.", session.Id);

        // ── Step 3: Generate clarifying questions via Gemini ──────────────────
        var aiRequest = new ClarifyingQuestionsRequest
        {
            Topic = request.Topic,
            LeftViewText = request.Person1View,
            RightViewText = request.Person2View
        };

        // Pass CancellationToken.None so the request pipeline's short-lived token
        // does not prematurely cancel the AI call. The service itself applies a 100s
        // independent timeout.
        var aiQuestions = await _aiService.GenerateClarifyingQuestionsAsync(
            aiRequest, CancellationToken.None);

        _logger.LogInformation(
            "Received {Count} clarifying questions from Gemini.", aiQuestions.Count);

        // ── Step 4: Persist questions and build response ──────────────────────
        var questionDtos = new List<QuestionDto>();

        foreach (var item in aiQuestions)
        {
            var sides = ResolveSides(item.TargetSide);

            foreach (var side in sides)
            {
                var question = ClarifyQuestion.Create(side, item.QuestionText, session.Id);
                await _context.ClarifyQuestions.AddAsync(question, cancellationToken);

                questionDtos.Add(new QuestionDto
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    ForSide = side == ParticipantSide.P1 ? "P1" : "P2"
                });
            }
        }

        // ── Step 5: Mark session as InProgress and save all questions ─────────
        session.UpdateStatus(DebateSessionStatus.InProgress);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "DebateSession {SessionId} is now InProgress with {QuestionCount} questions.",
            session.Id, questionDtos.Count);

        return new StartDebateResponse
        {
            DebateId = session.Id,
            Questions = questionDtos
        };
    }

    /// <summary>
    /// Maps AI's "Left"/"Right"/"Both" target side to one or two ParticipantSide values.
    /// "Both" produces a question entry for each participant.
    /// </summary>
    private static IEnumerable<ParticipantSide> ResolveSides(string targetSide) =>
        targetSide.Trim().ToLowerInvariant() switch
        {
            "left"  => [ParticipantSide.P1],
            "right" => [ParticipantSide.P2],
            "both"  => [ParticipantSide.P1, ParticipantSide.P2],
            _       => [ParticipantSide.P1, ParticipantSide.P2]
        };
}
