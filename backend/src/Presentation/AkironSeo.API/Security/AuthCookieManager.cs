namespace AkironSeo.API.Security;

public sealed class AuthCookieManager
{
    public const string AccessCookieName = "akiron_access";
    public const string RefreshCookieName = "akiron_refresh";

    private const string AccessCookiePath = "/api";
    private const string RefreshCookiePath = "/api/v1/auth";

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthCookieManager(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void WriteAccessCookie(HttpResponse response, string token)
    {
        var expirationMinutes = _configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 30);
        response.Cookies.Append(
            AccessCookieName,
            token,
            CreateOptions(AccessCookiePath, DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)));
    }

    public void WriteRefreshCookie(HttpResponse response, string token)
    {
        var expirationDays = _configuration.GetValue("Jwt:RefreshTokenExpirationDays", 7);
        response.Cookies.Append(
            RefreshCookieName,
            token,
            CreateOptions(RefreshCookiePath, DateTimeOffset.UtcNow.AddDays(expirationDays)));
    }

    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AccessCookieName, CreateOptions(AccessCookiePath, DateTimeOffset.UnixEpoch));
        response.Cookies.Delete(RefreshCookieName, CreateOptions(RefreshCookiePath, DateTimeOffset.UnixEpoch));
    }

    private CookieOptions CreateOptions(string path, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = _configuration.GetValue<bool?>("Auth:CookieSecure") ?? !_environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = path,
        Expires = expires
    };
}
