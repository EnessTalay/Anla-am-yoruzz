using Anlasalamiyoruz.Application.Common.Interfaces;
using Anlasalamiyoruz.Domain.Entities;
using Anlasalamiyoruz.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Anlasalamiyoruz.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<DebateSession> DebateSessions => Set<DebateSession>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ClarifyQuestion> ClarifyQuestions => Set<ClarifyQuestion>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<ActionStep> ActionSteps => Set<ActionStep>();
    public DbSet<EmotionTag> EmotionTags => Set<EmotionTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DebateSession
        modelBuilder.Entity<DebateSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);
        });

        // Participant — One DebateSession has many Participants
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ViewText).IsRequired();
            entity.Property(e => e.Side)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Participants)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ClarifyQuestion — One DebateSession has many ClarifyQuestions
        modelBuilder.Entity<ClarifyQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionText).IsRequired();
            entity.Property(e => e.Answer);
            entity.Property(e => e.ForSide)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.ClarifyQuestions)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AnalysisResult — One DebateSession has one AnalysisResult
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VennJson).IsRequired();
            entity.Property(e => e.VerdictText).IsRequired();
            entity.Property(e => e.SuggestionText).IsRequired();

            entity.HasOne(e => e.Session)
                  .WithOne(s => s.AnalysisResult)
                  .HasForeignKey<AnalysisResult>(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ActionStep — One DebateSession has many ActionSteps
        modelBuilder.Entity<ActionStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepText).IsRequired();
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.ActionSteps)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // EmotionTag — One DebateSession has many EmotionTags
        modelBuilder.Entity<EmotionTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmotionKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Side)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.EmotionTags)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
