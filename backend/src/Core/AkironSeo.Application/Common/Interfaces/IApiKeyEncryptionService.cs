namespace AkironSeo.Application.Common.Interfaces;

public interface IApiKeyEncryptionService
{
    string Encrypt(string plainTextKey);
    string Decrypt(string cipherTextWithNonceAndTag);
}
