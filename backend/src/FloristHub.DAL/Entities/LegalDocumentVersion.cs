using FloristHub.DAL.Enums;

namespace FloristHub.DAL.Entities;

/// <summary>Versão publicada de um documento legal da plataforma (texto-base).</summary>
public class LegalDocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public LegalDocumentType DocumentType { get; set; }
    /// <summary>Identificador legível da versão (ex. 2026.1).</summary>
    public required string Version { get; set; }
    public required string Title { get; set; }
    public required string ContentMarkdown { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCurrent { get; set; }
}
