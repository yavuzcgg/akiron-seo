namespace AkironSeo.Application.Common.Interfaces;

public interface IDnsLookupService
{
    /// <summary>
    /// Returns true when the domain publishes a TXT record whose value matches
    /// <paramref name="expectedValue"/> exactly.
    /// </summary>
    Task<bool> HasTxtRecordAsync(string domain, string expectedValue, CancellationToken cancellationToken = default);
}
