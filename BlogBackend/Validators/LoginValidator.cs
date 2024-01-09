using BlogBackend.DTOs;
using FluentValidation;

namespace BlogBackend.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(l => l.Email)
            .EmailAddress()
            .WithMessage("Invalid Entry");
        RuleFor(l => l.Password)
            .NotEmpty()
            .WithMessage("Invalid Entry");
    }
}