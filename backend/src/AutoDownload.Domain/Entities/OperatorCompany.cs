using AutoDownload.Domain.Common;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Domain.Entities;

public sealed class OperatorCompany : Entity
{
    public OperatorCompany(
        Guid id,
        string code,
        string name,
        ServiceType serviceType,
        Uri portalBaseUrl,
        bool isActive)
        : base(id)
    {
        Code = EnsureCode(code);
        Name = EnsureName(name);
        ServiceType = serviceType;
        PortalBaseUrl = portalBaseUrl;
        IsActive = isActive;
    }

    public string Code { get; }

    public string Name { get; private set; }

    public ServiceType ServiceType { get; private set; }

    public Uri PortalBaseUrl { get; private set; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string EnsureCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 50)
        {
            throw new DomainException("Operator code is invalid.");
        }

        return normalized;
    }

    private static string EnsureName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 120)
        {
            throw new DomainException("Operator name is invalid.");
        }

        return normalized;
    }
}
