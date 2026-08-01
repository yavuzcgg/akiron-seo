using System.Net;
using System.Net.Sockets;

namespace AkironSeo.Application.Common.Security;

/// <summary>
/// Raised when a tenant-supplied URL resolves somewhere the server must not fetch.
/// </summary>
public class UnsafeOutboundUrlException : Exception
{
    public UnsafeOutboundUrlException(string message) : base(message) { }
}

/// <summary>
/// Screens tenant-supplied URLs before the server fetches them.
///
/// Crawl targets and webhook endpoints are attacker-controlled strings, so without
/// this check they can point at loopback, link-local metadata endpoints (169.254.169.254),
/// or private RFC 1918 ranges and turn the API into a proxy into its own network.
/// </summary>
public static class OutboundUrlGuard
{
    /// <summary>
    /// Resolves the host and throws if the URL is malformed, uses a non-HTTP scheme,
    /// or resolves to any non-public address.
    /// </summary>
    public static async Task<Uri> EnsureSafeAsync(string url, CancellationToken cancellationToken = default)
    {
        var candidate = url.Contains("://", StringComparison.Ordinal) ? url : $"https://{url}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            throw new UnsafeOutboundUrlException($"'{url}' is not a valid absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new UnsafeOutboundUrlException($"Only http and https URLs are allowed, got '{uri.Scheme}'.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            throw new UnsafeOutboundUrlException($"Host '{uri.DnsSafeHost}' could not be resolved.");
        }

        if (addresses.Length == 0)
        {
            throw new UnsafeOutboundUrlException($"Host '{uri.DnsSafeHost}' could not be resolved.");
        }

        // Every resolved address must be public: a host with one public and one private
        // record could otherwise be used to slip past the check on a later connection.
        foreach (var address in addresses)
        {
            if (!IsPubliclyRoutable(address))
            {
                throw new UnsafeOutboundUrlException(
                    $"Host '{uri.DnsSafeHost}' resolves to the non-public address {address}.");
            }
        }

        return uri;
    }

    private static bool IsPubliclyRoutable(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal) return false;
            if (address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None)) return false;

            // IPv4-mapped addresses (::ffff:10.0.0.1) must be judged on the IPv4 value.
            if (address.IsIPv4MappedToIPv6) return IsPubliclyRoutable(address.MapToIPv4());

            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork) return false;

        var octets = address.GetAddressBytes();

        return octets[0] switch
        {
            0 => false,                                        // 0.0.0.0/8 "this network"
            10 => false,                                       // RFC 1918
            127 => false,                                      // loopback
            100 when octets[1] >= 64 && octets[1] <= 127 => false, // RFC 6598 carrier-grade NAT
            169 when octets[1] == 254 => false,                // link-local, includes cloud metadata
            172 when octets[1] >= 16 && octets[1] <= 31 => false,  // RFC 1918
            192 when octets[1] == 168 => false,                // RFC 1918
            192 when octets[1] == 0 && octets[2] == 0 => false, // IETF protocol assignments
            198 when octets[1] == 18 || octets[1] == 19 => false, // benchmarking
            >= 224 => false,                                   // multicast and reserved
            _ => true
        };
    }
}
