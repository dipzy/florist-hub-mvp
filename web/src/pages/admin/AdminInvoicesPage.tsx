import { useEffect, useState, type FormEvent } from 'react';
import { usePageText } from '../../lib/usePageText';
import { api } from '../../api/client';
import type { InvoicingSettings, SalesDocument } from '../../api/types';
import { INVOICE_KIND_LABELS, INVOICE_STATUS_LABELS } from '../../api/types';

import { ErrorState } from '../../components/ui/ErrorState';
import { LoadingState } from '../../components/ui/LoadingState';
import { formatMoney } from '../../lib/locale';
import { InlineSelect } from '../../components/ui/InlineSelect';

const VAT_OPTIONS = [
  { value: 23, label: "23% — Continente" },
  { value: 22, label: "22% — Madeira" },
  { value: 16, label: "16% — Açores" },
  { value: 13, label: "13% — taxa intermédia" },
  { value: 6, label: "6% — taxa reduzida" },
  { value: 0, label: "0% — isento" },
];

export function AdminInvoicesPage() {
  const { pt } = usePageText('pages/admin/AdminInvoicesPage');
  const [settings, setSettings] = useState<InvoicingSettings | null>(null);
  const [apiKey, setApiKey] = useState('');
  const [documents, setDocuments] = useState<SalesDocument[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    const [current, list] = await Promise.all([api.invoicingSettings(), api.invoices()]);
    setSettings(current);
    setDocuments(list);
  }

  useEffect(() => {
    load().catch(() => setError(pt("Não foi possível carregar a faturação.")));
  }, []);

  async function save(e: FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setMsg(null);
    setError(null);
    try {
      const saved = await api.updateInvoicingSettings({
        legalName: settings.legalName,
        taxId: settings.taxId,
        addressLine: settings.addressLine,
        city: settings.city,
        postalCode: settings.postalCode,
        country: settings.country || 'Portugal',
        provider: Number(settings.provider),
        accountName: settings.accountName || null,
        apiKey: apiKey.trim() || null,
        documentSeries: settings.documentSeries || null,
        defaultVatRate: Number(settings.defaultVatRate),
        taxExemptionCode: settings.taxExemptionCode || null,
        enabled: settings.enabled,
      });
      setSettings(saved);
      setApiKey('');
      setMsg('Dados de faturação guardados. A chave da API não volta a ser mostrada.');
      setDocuments(await api.invoices());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível guardar.');
    }
  }

  async function download(document: SalesDocument) {
    setError(null);
    try {
      await api.downloadInvoicePdf(document.id, `${document.documentNumber || 'documento'}.pdf`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível descarregar o PDF.');
    }
  }

  async function retry(id: string) {
    setError(null);
    try {
      await api.retryInvoice(id);
      setDocuments(await api.invoices());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível repetir a emissão.');
    }
  }

  if (!settings) {
    if (error) return <ErrorState message={error} onRetry={() => load().catch(() => undefined)} />;
    return <LoadingState message={pt("A carregar faturação…")} />;
  }

  return (
    <div className="page">
      <h1>Faturação</h1>
      <p className="muted">
        As faturas-recibo saem em nome desta florista, pelo software certificado configurado aqui. A comissão da plataforma
        não é faturada neste ecrã.
      </p>
      <form className="card form admin-form" onSubmit={save}>
        <label>
          Nome legal
          <input value={settings.legalName} onChange={(ev) => setSettings({ ...settings, legalName: ev.target.value })} required />
        </label>
        <label>
          NIF
          <input value={settings.taxId} onChange={(ev) => setSettings({ ...settings, taxId: ev.target.value })} required />
        </label>
        <label>
          Morada
          <input value={settings.addressLine} onChange={(ev) => setSettings({ ...settings, addressLine: ev.target.value })} required />
        </label>
        <label>
          Localidade
          <input value={settings.city} onChange={(ev) => setSettings({ ...settings, city: ev.target.value })} required />
        </label>
        <label>
          Código postal
          <input
            value={settings.postalCode}
            placeholder="0000-000"
            onChange={(ev) => setSettings({ ...settings, postalCode: ev.target.value })}
            required
          />
        </label>
        <label>
          Fornecedor
          <InlineSelect
            value={settings.provider}
            onChange={(ev) => setSettings({ ...settings, provider: Number(ev.target.value) })}
          >
            <option value={0}>Teste (sem valor fiscal)</option>
            <option value={1}>InvoiceXpress</option>
          </InlineSelect>
        </label>
        {Number(settings.provider) === 1 && (
          <>
            <label>
              Conta InvoiceXpress
              <input
                value={settings.accountName ?? ''}
                placeholder="nome-da-conta"
                onChange={(ev) => setSettings({ ...settings, accountName: ev.target.value })}
                required
              />
            </label>
            <label>
              Chave da API
              <input
                type="password"
                value={apiKey}
                autoComplete="new-password"
                placeholder={settings.credentialsConfigured ? 'Já guardada. Preencha só para substituir.' : 'Chave da API'}
                onChange={(ev) => setApiKey(ev.target.value)}
              />
            </label>
            <label>
              Série (identificador no InvoiceXpress, opcional)
              <input
                value={settings.documentSeries ?? ''}
                onChange={(ev) => setSettings({ ...settings, documentSeries: ev.target.value })}
              />
            </label>
          </>
        )}
        <label>
          IVA por defeito
          <InlineSelect
            value={settings.defaultVatRate}
            onChange={(ev) => setSettings({ ...settings, defaultVatRate: Number(ev.target.value) })}
          >
            {VAT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </InlineSelect>
        </label>
        <label>
          Código de isenção (se houver IVA a 0%)
          <input
            value={settings.taxExemptionCode ?? ''}
            placeholder="M10"
            onChange={(ev) => setSettings({ ...settings, taxExemptionCode: ev.target.value })}
          />
        </label>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={settings.enabled}
            onChange={(ev) => setSettings({ ...settings, enabled: ev.target.checked })}
          />
          Emitir automaticamente quando o pagamento é confirmado
        </label>
        <button type="submit">Guardar</button>
        {msg && <p className="muted">{msg}</p>}
        {error && <p className="muted">{error}</p>}
      </form>

      <h2>Documentos</h2>
      {documents.length === 0 ? (
        <p className="muted">Ainda não há faturas.</p>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Documento</th>
              <th>Encomenda</th>
              <th>Estado</th>
              <th>Total</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {documents.map((document) => (
              <tr key={document.id}>
                <td>
                  {INVOICE_KIND_LABELS[document.kind] ?? document.kind}
                  {document.documentNumber ? ` ${document.documentNumber}` : ''}
                  {document.lastError && document.status !== 1 && <div className="muted">{document.lastError}</div>}
                </td>
                <td>{document.orderId.slice(0, 8)}</td>
                <td>{INVOICE_STATUS_LABELS[document.status] ?? document.status}</td>
                <td>{formatMoney(document.grossAmount)}</td>
                <td>
                  {document.status === 1 && (
                    <button type="button" onClick={() => download(document)}>
                      PDF
                    </button>
                  )}
                  {document.status !== 1 && (
                    <button type="button" className="secondary" onClick={() => retry(document.id)}>
                      Repetir
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
