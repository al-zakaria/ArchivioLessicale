using ArchivioLessicale.API.Features.Auth;
using FluentValidation;

public record RegisterUserCommand(
    string Email,
    string Password,
    string NickName,
    string DisplayName,
    string FirstName, 
    string LastName, 
    UserGrade Grade,
    int NumberOfWordsStudied,
    int NumberOfWordsLearned,
    ClientMetaData ClientMetaData
);

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string NickName,
    string DisplayName
);

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения.")
            .EmailAddress().WithMessage("Некорректный формат Email.")
            .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов.")
            .Matches(@"[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву.")
            .Matches(@"[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву.")
            .Matches(@"[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру.")
            .Matches(@"[\!\?\*\.\@\#\$\%\^\&\+\=]").WithMessage("Пароль должен содержать хотя бы один спецсимвол (!?*.@#$%^&+=-).");

        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("Никнейм обязателен.")
            .MinimumLength(3).WithMessage("Никнейм должен быть от 3 символов.")
            .MaximumLength(30).WithMessage("Никнейм не должен превышать 30 символов.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Никнейм может содержать только латинские буквы, цифры и символ подчеркивания.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Отображаемое имя обязательно.")
            .MaximumLength(50).WithMessage("Отображаемое имя не должно превышать 50 символов.");

        RuleFor(x => x.FirstName)
            .MaximumLength(50).WithMessage("Имя не должно превышать 50 символов.");

        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Фамилия не должна превышать 50 символов.");

        RuleFor(x => x.Grade)
            .IsInEnum().WithMessage("Указан некорректный уровень пользователя (Grade).");

        RuleFor(x => x.NumberOfWordsStudied)
            .GreaterThanOrEqualTo(0).WithMessage("Количество изучаемых слов не может быть отрицательным.");

        RuleFor(x => x.NumberOfWordsLearned)
            .GreaterThanOrEqualTo(0).WithMessage("Количество выученных слов не может быть отрицательным.")
            .LessThanOrEqualTo(x => x.NumberOfWordsStudied)
            .WithMessage("Количество выученных слов не может превышать количество изучаемых.");

        RuleFor(x => x.ClientMetaData)
            .NotNull().WithMessage("Метаданные клиента обязательны.");
    }
}