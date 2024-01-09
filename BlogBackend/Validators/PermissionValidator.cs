using BlogBackend.DTOs;
using FluentValidation;

namespace BlogBackend.Validators;

public class PermissionValidator: AbstractValidator<PermissionDto>
{
    public PermissionValidator()
    {
        RuleFor(p => p.Email)
            .EmailAddress()
            .WithMessage("Invalid entry");
    }
}