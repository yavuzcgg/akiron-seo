using AkironSeo.Domain.Enums;
using FluentValidation;

namespace AkironSeo.Application.Auth.Dtos;

public record LoginRequestDto(string Email, string Password);
public record RegisterRequestDto(string TenantName, string Email, string Password, string FullName);
public record SessionDto(Guid UserId, string UserEmail, Guid TenantId, string Role);
public record SaveApiKeyDto(AiProviderEnum Provider, string ApiKey);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^A-Za-z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class SaveApiKeyValidator : AbstractValidator<SaveApiKeyDto>
{
    public SaveApiKeyValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.ApiKey).NotEmpty().MinimumLength(16).MaximumLength(4096);
    }
}
