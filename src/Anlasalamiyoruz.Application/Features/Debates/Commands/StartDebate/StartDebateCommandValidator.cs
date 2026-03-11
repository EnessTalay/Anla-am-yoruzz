using FluentValidation;

namespace Anlasalamiyoruz.Application.Features.Debates.Commands.StartDebate;

public class StartDebateCommandValidator : AbstractValidator<StartDebateCommand>
{
    public StartDebateCommandValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Anlaşmazlık konusu boş olamaz.")
            .MaximumLength(500).WithMessage("Konu en fazla 500 karakter olabilir.");

        RuleFor(x => x.Person1Name)
            .NotEmpty().WithMessage("1. kişinin adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Person1View)
            .NotEmpty().WithMessage("1. kişinin görüşü boş olamaz.")
            .MinimumLength(3).WithMessage("1. kişinin görüşü en az 3 karakter olmalıdır.")
            .MaximumLength(5000).WithMessage("Görüş en fazla 5000 karakter olabilir.");

        RuleFor(x => x.Person2Name)
            .NotEmpty().WithMessage("2. kişinin adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Person2View)
            .NotEmpty().WithMessage("2. kişinin görüşü boş olamaz.")
            .MinimumLength(3).WithMessage("2. kişinin görüşü en az 3 karakter olmalıdır.")
            .MaximumLength(5000).WithMessage("Görüş en fazla 5000 karakter olabilir.");
    }
}
