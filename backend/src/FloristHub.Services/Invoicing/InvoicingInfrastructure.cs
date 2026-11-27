using FloristHub.DAL.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Services.Invoicing;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("FloristHub.Invoicing.Credentials.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}

public sealed class InvoicingProviderFactory : IInvoicingProviderFactory
{
    private readonly IServiceProvider _services;

    public InvoicingProviderFactory(IServiceProvider services) => _services = services;

    public IInvoicingProvider Get(InvoicingProviderKind kind) => kind switch
    {
        InvoicingProviderKind.Fake => _services.GetRequiredService<FakeInvoicingProvider>(),
        InvoicingProviderKind.InvoiceXpress => _services.GetRequiredService<InvoiceXpressInvoicingProvider>(),
        _ => throw new InvalidOperationException("Fornecedor de faturação desconhecido.")
    };
}
