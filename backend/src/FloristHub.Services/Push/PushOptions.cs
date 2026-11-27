namespace FloristHub.Services.Push;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    public string Provider { get; set; } = "Log";

    public string? FcmProjectId { get; set; }

    /// <summary>JSON da conta de serviço, numa linha. Não commitar.</summary>
    public string? FcmServiceAccountJson { get; set; }

    /// <summary>Caminho local para o JSON da conta de serviço, fora do repositório.</summary>
    public string? FcmServiceAccountPath { get; set; }

    public int MaxSendAttempts { get; set; } = 3;

    public bool IsFcmReady()
    {
        if (!Provider.Equals("Fcm", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(FcmProjectId))
            return false;
        if (!string.IsNullOrWhiteSpace(FcmServiceAccountJson))
            return true;
        return !string.IsNullOrWhiteSpace(FcmServiceAccountPath) && File.Exists(FcmServiceAccountPath);
    }
}
