import { useMemo, useState, type FormEvent } from 'react';
import { usePageText } from '../../lib/usePageText';
import { useTranslation } from 'react-i18next';
import { api } from '../../api/client';
import type { CapacityDay, CapacitySettings, CapacityWindowInput, OpeningHour } from '../../api/types';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { capacityFormFromDays } from '../../lib/capacity';
import { coerceOpeningHours } from '../../lib/coerceApi';
import { CapacityCalendar } from './CapacityCalendar';

function isoDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function visibleBounds(anchor: Date) {
  const monthFrom = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
  const monthTo = new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const weekEnd = new Date(today);
  weekEnd.setDate(today.getDate() + 6);
  const from = monthFrom < today ? monthFrom : today;
  const to = monthTo > weekEnd ? monthTo : weekEnd;
  return { from: isoDate(from), to: isoDate(to) };
}

export function AdminCapacityPage() {
  const { pt } = usePageText('pages/admin/AdminCapacityPage');
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState(() => new Date());
  const [days, setDays] = useState<CapacityDay[]>([]);
  const [limit, setLimit] = useState<number | null>(null);
  const [windows, setWindows] = useState<CapacityWindowInput[]>([]);
  const [openingHours, setOpeningHours] = useState<OpeningHour[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  const bounds = useMemo(() => visibleBounds(anchor), [anchor]);

  function applyCapacity(settings: CapacitySettings | null, calendar: CapacityDay[]) {
    const derived = capacityFormFromDays(calendar);
    setDays(calendar);
    setLimit(settings?.dailyOrderLimit ?? derived.dailyOrderLimit);
    setWindows(settings && settings.windows.length > 0 ? settings.windows : derived.windows);
  }

  async function load() {
    const [settings, calendar, checkout] = await Promise.all([
      api.capacitySettings(),
      api.floristCapacity(bounds.from, bounds.to),
      api.storeCheckout(),
    ]);
    setOpeningHours(coerceOpeningHours(checkout));
    applyCapacity(settings, calendar);
    setLoaded(true);
  }

  const screen = useAdminLoad(() => load(), [bounds.from, bounds.to]);

  async function saveSettings(event: FormEvent) {
    event.preventDefault();
    setMsg(null);
    if (limit == null) {
      setMsg(pt("Não foi possível guardar o limite."));
      return;
    }
    try {
      await api.updateCapacitySettings({ dailyOrderLimit: limit, windows });
      setMsg('Limite guardado.');
      await load();
    } catch {
      setMsg(pt("Não foi possível guardar o limite."));
    }
  }

  if (screen.phase !== 'ready' || !loaded) {
    return <AdminLoadGate phase={screen.phase === 'error' ? 'error' : 'loading'} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Capacidade de entrega</h1>
      {msg && <p className="muted">{msg}</p>}
      <form className="card form admin-form" onSubmit={saveSettings}>
        <label>
          Limite diário de encomendas
          <input
            type="number"
            min={0}
            max={500}
            value={limit ?? ''}
            onChange={(ev) => setLimit(ev.target.value === '' ? null : Number(ev.target.value))}
          />
        </label>
        {windows.map((window, index) => (
          <div className="capacity-window-row" key={`${window.start}-${index}`}>
            <label>
              {t('admin.capacity.windowName')}
              <input
                value={window.label}
                onChange={(ev) => setWindows(windows.map((item, i) => (i === index ? { ...item, label: ev.target.value } : item)))}
              />
            </label>
            <label>
              {t('admin.capacity.windowStart')}
              <input
                value={window.start}
                onChange={(ev) => setWindows(windows.map((item, i) => (i === index ? { ...item, start: ev.target.value } : item)))}
                placeholder="09:00"
              />
            </label>
            <label>
              {t('admin.capacity.windowEnd')}
              <input
                value={window.end}
                onChange={(ev) => setWindows(windows.map((item, i) => (i === index ? { ...item, end: ev.target.value } : item)))}
                placeholder="11:59"
              />
            </label>
            <label>
              {t('admin.capacity.windowLimit')}
              <input
                type="number"
                min={0}
                value={window.orderLimit}
                onChange={(ev) =>
                  setWindows(windows.map((item, i) => (i === index ? { ...item, orderLimit: Number(ev.target.value) } : item)))
                }
              />
            </label>
            <button type="button" className="fh-btn fh-btn--secondary" onClick={() => setWindows(windows.filter((_, i) => i !== index))}>
              {t('admin.capacity.removeWindow')}
            </button>
          </div>
        ))}
        <button
          type="button"
          className="fh-btn fh-btn--secondary capacity-add-window"
          onClick={() => setWindows([...windows, { label: 'Janela', start: '09:00', end: '12:00', orderLimit: 2 }])}
        >
          Adicionar janela
        </button>
        <button type="submit">Guardar limite</button>
      </form>

      <CapacityCalendar
        anchor={anchor}
        days={days}
        openingHours={openingHours}
        onMonth={setAnchor}
        onSaveDay={async (input) => {
          setMsg(null);
          try {
            await api.upsertCapacityException({
              date: input.date,
              isClosed: input.closed,
              dailyOrderLimitOverride: input.closed || input.limit === '' ? null : Number(input.limit),
              note: input.note || null,
            });
            setMsg('Dia actualizado.');
            await load();
          } catch {
            setMsg(pt('Não foi possível guardar a exceção.'));
            throw new Error('save');
          }
        }}
      />
    </div>
  );
}
