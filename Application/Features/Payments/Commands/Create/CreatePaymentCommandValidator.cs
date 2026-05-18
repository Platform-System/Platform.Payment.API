using FluentValidation;

namespace Platform.Payment.API.Application.Features.Payments.Commands.Create;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Request.ReferenceType).NotEmpty();
        RuleFor(x => x.Request.ReferenceId).NotEmpty();
        RuleFor(x => x.Request.ReferenceCode).GreaterThan(0);
        RuleFor(x => x.Request.Provider).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Currency).NotEmpty();
        RuleFor(x => x.Request.Description).NotEmpty();
        RuleFor(x => x.Request.Items).NotEmpty();
    }
}
