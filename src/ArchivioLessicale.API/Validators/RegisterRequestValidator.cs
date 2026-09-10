using ArchivioLessicale.API.Models.DTOs;
using ArchivioLessicale.API.Models.DTOs.Auth.Register;
using FluentValidation;

namespace ArchivioLessicale.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(rule => rule.DisplayName)
            .NotEmpty().WithMessage("Display name is required");

        RuleFor(rule => rule.NickName)
            .NotEmpty().WithMessage("Nickname is required");

        RuleFor(rule => rule.Grade)
            .NotEmpty().WithMessage("Grade is required");

        RuleFor(rule => rule.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(rule => rule.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");
        RuleFor(rule => rule.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(rule => rule.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(password => password.Password).WithMessage("Passwords do not match");
    }
}
