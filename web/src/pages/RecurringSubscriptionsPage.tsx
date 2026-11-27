import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { RecurringSubscription } from '../api/types';
import { RECURRING_FREQUENCY_LABELS, RECURRING_STATUS_LABELS, DELIVERY_LABELS } from '../api/types';
import { Button } from '../components/ui/Button';
import { LegalAcceptanceFields } from '../components/LegalAcceptanceFields';
import type { LegalVersions } from '../api/types';
import { buildLegalAcceptance } from '../lib/legalConsent';
import { formatDateTime, formatMoney } from '../lib/locale';
import { InlineSelect } from '../components/ui/InlineSelect';

const frequencies = [
  { value: 0, label: RECURRING_FREQUENCY_LABELS[0] },
  { value: 1, label: RECURRING_FREQUENCY_LABELS[1] },
  { value: 2, label: RECURRING_FREQUENCY_LABELS[2] },
];

export function RecurringSubscriptionsPage() {
  const [items, setItems] = useState<RecurringSubscription[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [frequency, setFrequency] = useState(0);
  const [deliveryType, setDeliveryType] = useState(0);
  const [anchor, setAnchor] = useState('');
  const [address, setAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [floristChoice, setFloristChoice] = useState(true);
  const [budget, setBudget] = useState('45');
  const [legalVersions, setLegalVersions] = useState<LegalVersions | null>(null);
  const [acceptLegal, setAcceptLegal] = useState(false);

  const reload = useCallback(() => {
    api
      .recurringSubscriptions()
      .then(setItems)
      .catch(() => setErr('Não foi possível carregar as recorrências.'));
  }, []);

  useEffect(() => {
    reload();
    api.legalVersions().then(setLegalVersions).catch(() => setLegalVersions(null));
  }, [reload]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    try {
      if (!legalVersions || !acceptLegal) {
        setErr('Tem de aceitar os Termos e a Política de Privacidade.');
        return;
      }
      const legal = buildLegalAcceptance(legalVersions, acceptLegal);
      await api.createRecurringSubscription({
        frequency,
        anchorDeliveryAt: new Date(anchor).toISOString(),
        deliveryType,
        deliveryAddress: deliveryType === 0 ? null : address,
        deliveryZoneId: null,
        baseType: 1,
        customerNotes: notes || null,
        isFloristChoice: floristChoice,
        floristChoiceBudget: floristChoice ? Number(budget) : null,
        items: floristChoice ? [] : [],
        acceptTermsAndPrivacy: legal.acceptTermsAndPrivacy,
        termsVersion: legal.termsVersion,
        privacyVersion: legal.privacyVersion,
      });
      setShowForm(false);
      reload();
    } catch {
      setErr('Não foi possível criar a recorrência. Verifique os dados.');
    }
  }

  async function act(id: string, action: 'pause' | 'resume' | 'skip' | 'cancel') {
    const fn =
      action === 'pause'
        ? api.pauseRecurring
        : action === 'resume'
          ? api.resumeRecurring
          : action === 'skip'
            ? api.skipRecurringDelivery
            : api.cancelRecurring;
  await fn(id);
    reload();
  }

  return (
    <div className="page">
      <h1>Recorrências</h1>
      <p className="muted">Subscrições de flores para o escritório, restaurante ou espaço que precise de entregas regulares.</p>
      {err && <p className="error">{err}</p>}
      <Button type="button" onClick={() => setShowForm((v) => !v)}>
        {showForm ? 'Fechar formulário' : 'Nova recorrência'}
      </Button>

      {showForm && (
        <form className="card form-stack" onSubmit={submit}>
          <label>
            Frequência
            <InlineSelect value={frequency} onChange={(e) => setFrequency(Number(e.target.value))}>
              {frequencies.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </InlineSelect>
          </label>
          <label>
            Primeira entrega
            <input type="datetime-local" required value={anchor} onChange={(e) => setAnchor(e.target.value)} />
          </label>
          <label>
            Entrega
            <InlineSelect value={deliveryType} onChange={(e) => setDeliveryType(Number(e.target.value))}>
              {DELIVERY_LABELS.map((label, idx) => (
                <option key={label} value={idx}>
                  {label}
                </option>
              ))}
            </InlineSelect>
          </label>
          {deliveryType !== 0 && (
            <label>
              Morada
              <input value={address} onChange={(e) => setAddress(e.target.value)} required />
            </label>
          )}
          <label className="checkbox">
            <input type="checkbox" checked={floristChoice} onChange={(e) => setFloristChoice(e.target.checked)} />
            Escolha da florista (com orçamento)
          </label>
          {floristChoice && (
            <label>
              Orçamento máximo (€)
              <input type="number" min="1" step="0.01" value={budget} onChange={(e) => setBudget(e.target.value)} />
            </label>
          )}
          <label>
            Notas
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} />
          </label>
          <LegalAcceptanceFields versions={legalVersions} checked={acceptLegal} onCheckedChange={setAcceptLegal} />
          <Button type="submit">Pedir subscrição</Button>
        </form>
      )}

      <div className="stack gap-md" style={{ marginTop: '1.5rem' }}>
        {items.map((s) => (
          <article key={s.id} className="card">
            <header className="row spread">
              <strong>{RECURRING_FREQUENCY_LABELS[s.frequency] ?? 'Recorrência'}</strong>
              <span className="badge">{RECURRING_STATUS_LABELS[s.status]}</span>
            </header>
            <p className="muted">
              Próxima entrega:{' '}
              {s.nextDeliveryAt ? formatDateTime(s.nextDeliveryAt) : '—'}
            </p>
            <p>
              Estimativa: {formatMoney(s.estimatedItemsTotal + s.deliveryFee)}
              {s.isFloristChoice ? ' (escolha da florista)' : ''}
            </p>
            <div className="row gap-sm">
              {s.status === 1 && (
                <>
                  <Button type="button" variant="secondary" size="sm" onClick={() => act(s.id, 'pause')}>
                    Pausar
                  </Button>
                  <Button type="button" variant="secondary" size="sm" onClick={() => act(s.id, 'skip')}>
                    Saltar entrega
                  </Button>
                </>
              )}
              {s.status === 2 && (
                <Button type="button" size="sm" onClick={() => act(s.id, 'resume')}>
                  Retomar
                </Button>
              )}
              {s.status !== 3 && s.status !== 4 && (
                <Button type="button" variant="danger" size="sm" onClick={() => act(s.id, 'cancel')}>
                  Cancelar
                </Button>
              )}
            </div>
          </article>
        ))}
        {items.length === 0 && !err && <p className="muted">Ainda não tem recorrências.</p>}
      </div>
    </div>
  );
}
