import { useEffect, useState, type FormEvent } from 'react';
import { usePageText } from '../../lib/usePageText';
import { api } from '../../api/client';

export function SuperAdminBillingPage() {
  const { pt } = usePageText('pages/superadmin/SuperAdminBillingPage');
  const [commission, setCommission] = useState('10');
  const [monthly, setMonthly] = useState('0');
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    api
      .platformBilling()
      .then((b) => {
        setCommission(String(b.commissionPercent));
        setMonthly(String(b.monthlyFeeAmount));
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar a comissão.'));
  }, []);

  async function save(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setMsg(null);
    const commissionPercent = Number(commission.replace(',', '.'));
    const monthlyFeeAmount = Number(monthly.replace(',', '.'));
    try {
      const saved = await api.updatePlatformBilling(commissionPercent, monthlyFeeAmount);
      setCommission(String(saved.commissionPercent));
      setMonthly(String(saved.monthlyFeeAmount));
      setMsg('Comissão global guardada. A mensalidade fica registada e ainda não é cobrada.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao guardar.');
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <h1>Comissão da plataforma</h1>
        <p className="lede muted">
          Percentagem aplicada em cada pagamento Stripe (application fee), salvo se a loja tiver um valor próprio. A
          mensalidade está preparada e não é debitada.
        </p>
      </header>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {msg && <p className="success">{msg}</p>}
      <form className="card form admin-form" onSubmit={save}>
        <label>
          Comissão global (%)
          <input value={commission} onChange={(e) => setCommission(e.target.value)} required />
        </label>
        <label>
          Mensalidade prevista (€)
          <input value={monthly} onChange={(e) => setMonthly(e.target.value)} required />
        </label>
        <button type="submit" className={pt("fh-btn fh-btn--primary fh-btn--md")}>
          Guardar
        </button>
      </form>
    </div>
  );
}
