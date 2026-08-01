using AkironSeo.Application.Common.Interfaces;
using DnsClient;
using Microsoft.Extensions.Logging;

namespace AkironSeo.Infrastructure.Services;

public class DnsLookupService : IDnsLookupService
{
    private readonly ILookupClient _lookupClient;
    private readonly ILogger<DnsLookupService> _logger;

    public DnsLookupService(ILookupClient lookupClient, ILogger<DnsLookupService> logger)
    {
        _lookupClient = lookupClient;
        _logger = logger;
    }

    public async Task<bool> HasTxtRecordAsync(
        string domain, string expectedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        var hostname = NormalizeToHostname(domain);

        try
        {
            var result = await _lookupClient.QueryAsync(hostname, QueryType.TXT, cancellationToken: cancellationToken);

            if (result.HasError)
            {
                _logger.LogInformation(
                    "TXT lookup for {Domain} returned an error: {Error}", hostname, result.ErrorMessage);
                return false;
            }

            // A TXT record can be split into multiple strings; each is compared on its own
            // because publishers frequently enter the token as a single unsplit value.
            return result.Answers.TxtRecords()
                .SelectMany(record => record.Text)
                .Any(value => string.Equals(value.Trim(), expectedValue, StringComparison.Ordinal));
        }
        catch (DnsResponseException ex)
        {
            _logger.LogInformation(ex, "TXT lookup for {Domain} failed.", hostname);
            return false;
        }
    }

    /// <summary>Strips scheme, path, and www so a stored DomainUrl can be queried directly.</summary>
    private static string NormalizeToHostname(string domain)
    {
        var value = domain.Trim();

        if (Uri.TryCreate(
                value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}",
                UriKind.Absolute,
                out var uri))
        {
            value = uri.Host;
        }

        return value.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? value[4..] : value;
    }
}
