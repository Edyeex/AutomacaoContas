using AutoDownload.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AutoDownload.Infrastructure.Security;

internal sealed class CredentialEncryptionOptions
{
    public string? Key { get; init; }
}

internal sealed class DataProtectionCredentialProtector : ICredentialProtector
{
    private const string AesPrefix = "aesgcm:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IDataProtector protector;
    private readonly byte[]? encryptionKey;

    public DataProtectionCredentialProtector(
        IDataProtectionProvider provider,
        IOptions<CredentialEncryptionOptions> options)
    {
        protector = provider.CreateProtector("AutoDownload.PortalCredentials.v1");
        encryptionKey = DecodeKey(options.Value.Key);
    }

    public string Protect(string plainText)
    {
        if (encryptionKey is null)
        {
            return protector.Protect(plainText);
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(encryptionKey, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

        return AesPrefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedText)
    {
        if (!protectedText.StartsWith(AesPrefix, StringComparison.Ordinal))
        {
            try
            {
                return protector.Unprotect(protectedText);
            }
            catch (Exception ex) when (ex is not CryptographicException)
            {
                throw new CryptographicException("Stored portal credential could not be read.", ex);
            }
        }

        if (encryptionKey is null)
        {
            throw new CryptographicException("Security:CredentialEncryption:Key is required to read portal credentials.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(protectedText[AesPrefix.Length..]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Stored portal credential payload is invalid.", ex);
        }

        if (payload.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("Stored portal credential payload is invalid.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipherBytes = payload.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(encryptionKey, TagSize);
        try
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new CryptographicException("Stored portal credential could not be read.", ex);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[]? DecodeKey(string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return null;
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "Security:CredentialEncryption:Key must be a Base64 value with 32 bytes.",
                ex);
        }

        if (key.Length != 32)
        {
            throw new InvalidOperationException(
                "Security:CredentialEncryption:Key must contain exactly 32 bytes after Base64 decoding.");
        }

        return key;
    }
}
