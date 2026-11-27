namespace FloristHub.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Log";

    public string? ResendApiKey { get; set; }

    public string From { get; set; } = "FloristHub <onboarding@resend.dev>";

    /// <summary>URL base do frontend (ex. https://loja.pages.dev) para links nas mensagens.</summary>
    public string? WebBaseUrl { get; set; }

    public int MaxSendAttempts { get; set; } = 3;
}
