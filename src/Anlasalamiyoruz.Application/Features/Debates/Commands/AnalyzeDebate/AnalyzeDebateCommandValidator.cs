using FluentValidation;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.AnalyzeDebate;

public class AnalyzeDebateCommandValidator : AbstractValidator<AnalyzeDebateCommand>
{
    public AnalyzeDebateCommandValidator()
    {
        RuleFor(x => x.DebateId)
            .NotEmpty().WithMessage("Oturum ID boş olamaz.");

        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Cevap listesi null olamaz.")
            .Must(a => a.Count > 0).WithMessage("En az bir soru cevabı gönderilmelidir.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId)
                .NotEmpty().WithMessage("Soru ID boş olamaz.");

            answer.RuleFor(a => a.AnswerText)
                .NotEmpty().WithMessage("Cevap metni boş olamaz.")
                .MaximumLength(2000).WithMessage("Cevap en fazla 2000 karakter olabilir.");
        });
    }
}
