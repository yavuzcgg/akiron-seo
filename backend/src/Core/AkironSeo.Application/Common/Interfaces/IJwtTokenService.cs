using System.Security.Claims;

namespace AkironSeo.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, Guid tenantId, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
