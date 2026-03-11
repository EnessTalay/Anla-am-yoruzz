using Anlasalamiyoruz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Anlasalamiyoruz.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<DebateSession> DebateSessions { get; }
    DbSet<Participant> Participants { get; }
    DbSet<ClarifyQuestion> ClarifyQuestions { get; }
    DbSet<AnalysisResult> AnalysisResults { get; }
    DbSet<ActionStep> ActionSteps { get; }
    DbSet<EmotionTag> EmotionTags { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
