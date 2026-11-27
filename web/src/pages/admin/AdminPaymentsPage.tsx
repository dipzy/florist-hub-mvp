import { useState } from 'react';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api } from '../../api/client';
import type { PaymentRecord, StripeConnectStatus } from '../../api/types';
import { PAYMENT_STATUS_LABELS } from '../../api/types';
import { formatDateTime, formatMoney } from '../../lib/locale';

export function AdminPaymentsPage() {
  const [stripe, setStripe] = useState<StripeConnectStatus | null>(null);
  const [payments, setPayments] = useState<PaymentRecord[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [amounts, setAmounts] = useState<Record<string, string>>({});

  async function load() {
    const [status, list] = await Promise.all([api.floristStripe(), api.floristPayments()]);
    setStripe(status);
    setPayments(list);
  }

  const screen = useAdminLoad(() => load(), []);

  async function connect() {
    setError(null);
    setMsg(null);
    const origin = window.location.origin;
    try {
      const res = await api.startStripeOnboarding(`${origin}/admin/pagamentos`, `${origin}/admin/pagamentos`);
      if (res.url) {
        window.location.assign(res.url);
        return;
      }
      setMsg(res.message ?? 'Não foi possível abrir a ligação Stripe.');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Falha na ligação Stripe.');
    }
  }

  async function refund(payment: PaymentRecord) {
    setError(null);
    setMsg(null);
    const raw = amounts[payment.id]?.trim();
    const amount = raw ? Number(raw.replace(',', '.')) : undefined;
    if (raw && (!Number.isFinite(amount) || (amount ?? 0) <= 0)) {
      setError('Indique um valor de reembolso válido, ou deixe vazio para o total em aberto.');
      return;
    }
    if (!window.confirm(amount ? `Reembolsar ${formatMoney(amount)}?` : 'Reembolsar o valor em aberto?')) return;
    try {
      await api.refundPayment(payment.id, amount, 'Reembolso no painel');
      setMsg('Reembolso pedido ao Stripe.');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Falha no reembolso.');
    }
  }

  const methods = [
    stripe?.cardPayments ? 'cartão' : null,
    stripe?.mbWay ? 'MB Way' : null,
    stripe?.multibanco ? 'Multibanco' : null,
  ].filter(Boolean);

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <section className="page">
      <h1>Pagamentos</h1>
      <p className="muted">
        O cliente paga depois de a encomenda ser confirmada. O valor entra na conta Stripe da loja e a plataforma fica com a comissão.
      </p>
      {error && <p className="error">{error}</p>}
      {msg && <p className="muted">{msg}</p>}

      <article className="card stack">
        <h2>Conta Stripe</h2>
        {!stripe?.paymentsEnabled && <p>Os pagamentos Stripe estão desativados neste ambiente.</p>}
        {stripe?.paymentsEnabled && (
          <>
            <p>
              {stripe.chargesEnabled
                ? 'A conta está pronta a receber pagamentos.'
                : stripe.connected
                  ? 'A conta está ligada, mas o Stripe ainda não autorizou cobranças.'
                  : 'Ainda não há conta Stripe ligada a esta loja.'}
            </p>
            {stripe.accountId && <p className="muted">Conta: {stripe.accountId}</p>}
            {methods.length > 0 && <p className="muted">Métodos ativos: {methods.join(', ')}.</p>}
            <button type="button" onClick={connect}>
              {stripe.connected ? 'Continuar ligação Stripe' : 'Ligar conta Stripe'}
            </button>
          </>
        )}
      </article>

      <h2>Movimentos</h2>
      {payments.length === 0 && <p className="muted">Ainda não há pagamentos.</p>}
      <div className="stack">
        {payments.map((p) => {
          const open = p.amount - p.refundedAmount;
          const refundable = p.status === 1 || p.status === 3;
          return (
            <article key={p.id} className="card stack">
              <p>
                <strong>Encomenda #{p.orderId.slice(0, 8)}</strong> · {PAYMENT_STATUS_LABELS[p.status] ?? p.status}
              </p>
              <p className="muted">
                {formatMoney(p.amount)} · comissão {p.commissionPercent}% ({formatMoney(p.applicationFeeAmountCents / 100)})
                {p.refundedAmount > 0 ? ` · reembolsado ${formatMoney(p.refundedAmount)}` : ''}
              </p>
              {p.failureMessage && <p className="error">{p.failureMessage}</p>}
              <ul className="list compact">
                {p.history.map((h) => (
                  <li key={h.id}>
                    {formatDateTime(h.createdAt)} — {h.notes ?? h.eventType}
                  </li>
                ))}
              </ul>
              {refundable && open > 0 && (
                <div className="row">
                  <input
                    inputMode="decimal"
                    placeholder={`Até ${formatMoney(open)} (vazio = total)`}
                    value={amounts[p.id] ?? ''}
                    onChange={(e) => setAmounts((prev) => ({ ...prev, [p.id]: e.target.value }))}
                    aria-label="Valor do reembolso"
                  />
                  <button type="button" className="secondary" onClick={() => refund(p)}>
                    Reembolsar
                  </button>
                </div>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}
