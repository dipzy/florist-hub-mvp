using FloristHub.DAL.Entities;
using FloristHub.Services.Email.Templates;

namespace FloristHub.Services.Email;

public static class FloristInviteEmailComposer
{
    public const int InviteValidHours = 72;

    public static string BuildSetPasswordUrl(string? webBaseUrl, Guid userId, string token)
    {
        var baseUrl = string.IsNullOrWhiteSpace(webBaseUrl)
            ? "http://localhost:5173"
            : webBaseUrl.TrimEnd('/');

        var query =
            $"userId={Uri.EscapeDataString(userId.ToString())}&token={Uri.EscapeDataString(token)}";
        return $"{baseUrl}/convite?{query}";
    }

    public static (string Subject, string Html, string SetPasswordUrl) Compose(
        Tenant tenant,
        ApplicationUser florist,
        string? webBaseUrl,
        string resetToken)
    {
        var url = BuildSetPasswordUrl(webBaseUrl, florist.Id, resetToken);
        var (subject, html) = TransactionalEmailTemplates.FloristStoreInvite(
            tenant,
            florist,
            url,
            InviteValidHours);
        return (subject, html, url);
    }
}
