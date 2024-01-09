using BlogBackend.DTOs;
using FluentValidation;

namespace BlogBackend.Validators;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(r => r.Email)
            .EmailAddress()
            .WithMessage("Invalid entry");
        RuleFor(r => r.Username)
            .NotEmpty()
            .WithMessage("Invalid entry")
            .MaximumLength(20)
            .WithMessage("This name is too long");
        RuleFor(r => r.Password)
            .NotEmpty()
            .WithMessage("Invalid entry")
            .Equal(r => r.ConfirmPassword)
            .WithMessage("Passwords don't match");
        RuleFor(r => r.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Invalid entry");
    }
}