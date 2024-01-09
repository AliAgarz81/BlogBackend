using BlogBackend.Data.Enums;
using BlogBackend.DTOs;
using FluentValidation;

namespace BlogBackend.Validators;

public class BlogValidator : AbstractValidator<BlogDto>
{
    public BlogValidator()
    {
        RuleFor(b => b.Title)
            .NotEmpty()
            .WithMessage("Title cannot be blank")
            .MaximumLength(30)
            .WithMessage("This title is too long! Maximum length is 30.");
        RuleFor(b => b.Text)
            .NotEmpty()
            .WithMessage("Text cannot be blank");
        RuleFor(b => b.Category)
            .NotEmpty()
            .WithMessage("Invalid category");
    }
}