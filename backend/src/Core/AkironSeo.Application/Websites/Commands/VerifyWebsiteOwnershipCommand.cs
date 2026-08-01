using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Common.Security;
using AkironSeo.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Application.Websites.Commands;

public record VerifyWebsiteOwnershipCommand(Guid WebsiteId, VerificationMethodEnum Method) : IRequest<bool>;

public class VerifyWebsiteOwnershipCommandHandler : IRequestHandler<VerifyWebsiteOwnershipCommand, bool>
{
    private readonly IAkironDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly HttpClient _httpClient;
    private readonly IDnsLookupService _dnsLookupService;

    public VerifyWebsiteOwnershipCommandHandler(
        IAkironDbContext dbContext,
        ITenantContext tenantContext,
        HttpClient httpClient,
        IDnsLookupService dnsLookupService)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _httpClient = httpClient;
        _dnsLookupService = dnsLookupService;
    }

    public async Task<bool> Handle(VerifyWebsiteOwnershipCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == request.WebsiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new KeyNotFoundException("Website not found.");
        }

        if (website.IsVerified) return true;

        bool verified = false;

        if (request.Method == VerificationMethodEnum.MetaTag)
        {
            try
            {
                var targetUrl = await OutboundUrlGuard.EnsureSafeAsync(website.DomainUrl, cancellationToken);
                var html = await _httpClient.GetStringAsync(targetUrl, cancellationToken);
                var expectedMeta = $"<meta name=\"akiron-site-verification\" content=\"{website.VerificationToken}\"";
                verified = html.Contains(expectedMeta, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                verified = false;
            }
        }
        else if (request.Method == VerificationMethodEnum.DnsTxt)
        {
            verified = await _dnsLookupService.HasTxtRecordAsync(
                website.DomainUrl, website.VerificationToken, cancellationToken);
        }

        if (verified)
        {
            website.IsVerified = true;
            website.VerifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return verified;
    }
}
