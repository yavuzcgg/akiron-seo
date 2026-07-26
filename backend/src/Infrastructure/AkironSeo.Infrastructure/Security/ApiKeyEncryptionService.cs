using System.Security.Cryptography;
using System.Text;
using AkironSeo.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AkironSeo.Infrastructure.Security;

public class ApiKeyEncryptionService : IApiKeyEncryptionService
{
    private readonly byte[] _masterKey;

    public ApiKeyEncryptionService(IConfiguration configuration)
    {
        var secret = configuration["Security:MasterEncryptionKey"]
            ?? throw new InvalidOperationException("Security:MasterEncryptionKey is not configured.");
        using var sha256 = SHA256.Create();
        _masterKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainTextKey)
    {
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return string.Empty;

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainTextKey);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aesGcm = new AesGcm(_masterKey, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Combined payload: nonce (12 bytes) + tag (16 bytes) + cipherBytes
        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string cipherTextWithNonceAndTag)
    {
        if (string.IsNullOrWhiteSpace(cipherTextWithNonceAndTag))
            return string.Empty;

        var combined = Convert.FromBase64String(cipherTextWithNonceAndTag);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        if (combined.Length < nonceSize + tagSize)
            throw new ArgumentException("Invalid cipher payload length.", nameof(cipherTextWithNonceAndTag));

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var cipherBytes = new byte[combined.Length - nonceSize - tagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(combined, nonceSize + tagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(_masterKey, tagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
