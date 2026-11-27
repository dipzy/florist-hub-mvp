namespace FloristHub.Services.Interfaces;

/// <summary>Invalidação do cache de domínios CORS (implementado na API).</summary>
public interface ICorsDomainCacheInvalidator
{
    void Invalidate();
}
