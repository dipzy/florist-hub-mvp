import { useState } from 'react';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api } from '../../api/client';
import type { FloristUpcomingRecurring, RecurringSubscription } from '../../api/types';
import { DELIVERY_LABELS, RECURRING_FREQUENCY_LABELS } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { formatDateTime, formatMoney } from '../../lib/locale';

export function AdminRecurringPage() {
  const [pending, setPending] = useState<RecurringSubscription[]>([]);
  const [upcoming, setUpcoming] = useState<FloristUpcomingRecurring[]>([]);
  const [err, setErr] = useState<string | null>(null);

  async function reload() {
    const [p, u] = await Promise.all([api.floristRecurring(0), api.floristRecurringUpcoming(21)]);
    setPending(p);
    setUpcoming(u);
    setErr(null);
  }

  const screen = useAdminLoad(() => reload(), []);

  async function accept(id: string) {
    await api.acceptRecurring(id);
    await reload().catch(() => setErr('Não foi possível carregar recorrências.'));
  }

  async function reject(id: string) {
    await api.rejectRecurring(id, 'Indisponível neste momento');
    await reload().catch(() => setErr('Não foi possível carregar recorrências.'));
  }

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Recorrências</h1>
      {err && <p className="muted">{err}</p>}
      <section>
        <h2>Próximas entregas (21 dias)</h2>
        <table className="data-table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Cliente</th>
              <th>Entrega</th>
              <th>Notas</th>
            </tr>
          </thead>
          <tbody>
            {upcoming.map((row) => (
              <tr key={`${row.subscriptionId}-${row.scheduledDeliveryAt}`}>
                <td>{formatDateTime(row.scheduledDeliveryAt)}</td>
                <td>{row.customerName ?? '—'}</td>
                <td>
                  {DELIVERY_LABELS[row.deliveryType] ?? '—'}
                  {row.isFloristChoice ? ' · Escolha da florista' : ''}
                </td>
                <td>{row.deliveryAddress ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {upcoming.length === 0 && <p className="muted">Sem entregas recorrentes previstas.</p>}
      </section>

      <section style={{ marginTop: '2rem' }}>
        <h2>Pedidos por aceitar</h2>
        {pending.map((s) => (
          <article key={s.id} className="card" style={{ marginBottom: '1rem' }}>
            <p>
              <strong>{s.customerName}</strong> · {RECURRING_FREQUENCY_LABELS[s.frequency]}
            </p>
            <p className="muted">Primeira entrega: {formatDateTime(s.anchorDeliveryAt)}</p>
            <p>
              {s.isFloristChoice
                ? `Escolha da florista até ${formatMoney(s.floristChoiceBudget ?? 0)}`
                : `${s.items.length} linhas de composição`}
            </p>
            <div className="row gap-sm">
              <Button type="button" size="sm" onClick={() => accept(s.id)}>
                Aceitar
              </Button>
              <Button type="button" variant="secondary" size="sm" onClick={() => reject(s.id)}>
                Recusar
              </Button>
            </div>
          </article>
        ))}
        {pending.length === 0 && <p className="muted">Não há pedidos pendentes.</p>}
      </section>
    </div>
  );
}
