using System.Security.Claims;

namespace AkironSeo.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, Guid tenantId, string role);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
