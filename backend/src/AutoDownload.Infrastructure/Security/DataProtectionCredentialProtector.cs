using AutoDownload.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace AutoDownload.Infrastructure.Security;

internal sealed class DataProtectionCredentialProtector : ICredentialProtector
{
    private readonly IDataProtector protector;

    public DataProtectionCredentialProtector(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("AutoDownload.PortalCredentials.v1");
    }

    public string Protect(string plainText) => protector.Protect(plainText);

    public string Unprotect(string protectedText) => protector.Unprotect(protectedText);
}
