namespace DocumentIngestion.Application;

public sealed class TenantSecurityGuard
{
    public TenantSecurityContext Authenticate(string? userId, string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation.");
        }

        return new TenantSecurityContext(userId, tenantId);
    }

    public void Authorize(TenantSecurityContext context, string requestedTenantId)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(context.TenantId, requestedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Authorization must ensure the caller can ingest documents for the specified tenant.");
        }
    }
}

public sealed record TenantSecurityContext(string UserId, string TenantId);
