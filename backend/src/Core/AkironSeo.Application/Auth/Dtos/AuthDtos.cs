namespace AkironSeo.Application.Auth.Dtos;

public record LoginRequestDto(string Email, string Password);
public record RegisterRequestDto(string TenantName, string Email, string Password, string FullName);
public record AuthResponseDto(bool Success, string Message, string? AccessToken, Guid? TenantId, string? UserEmail, string? Role);
