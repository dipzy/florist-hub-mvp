import { useMemo, useState } from 'react';
import type { CapacityDay, OpeningHour } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { capacityDayKey, capacityTone, closedByOpeningHours } from '../../lib/capacity';

type SaveDay = {
  date: string;
  closed: boolean;
  limit: string;
  note: string;
};

type Props = {
  anchor: Date;
  days: CapacityDay[];
  openingHours?: OpeningHour[];
  onMonth: (next: Date) => void;
  onSaveDay: (input: SaveDay) => Promise<void>;
};

function isoDate(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function toneFor(day: CapacityDay | undefined, dateIso: string, openingHours: OpeningHour[]) {
  return capacityTone(day, { closedByHours: closedByOpeningHours(dateIso, openingHours) });
}

function daySummary(day: CapacityDay | undefined, tone: string) {
  if (tone === 'closed') return 'Fechado';
  if (!day || tone === 'unknown') return 'Sem dados';
  return `${day.booked}/${day.dailyLimit ?? '∞'}`;
}

const WEEKDAYS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

export function CapacityCalendar({ anchor, days, openingHours = [], onMonth, onSaveDay }: Props) {
  const today = isoDate(new Date());
  const byDate = useMemo(() => {
    const map = new Map<string, CapacityDay>();
    for (const day of days) {
      const key = capacityDayKey(day.date);
      if (key) map.set(key, { ...day, date: key });
    }
    return map;
  }, [days]);
  const [editing, setEditing] = useState<CapacityDay | null>(null);
  const [closed, setClosed] = useState(false);
  const [limit, setLimit] = useState('');
  const [note, setNote] = useState('');
  const [saving, setSaving] = useState(false);

  const cells = useMemo(() => {
    const first = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
    const startOffset = (first.getDay() + 6) % 7;
    const count = new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0).getDate();
    const leading = Array.from({ length: startOffset }, () => null);
    const dates = Array.from({ length: count }, (_, index) => {
      const date = new Date(anchor.getFullYear(), anchor.getMonth(), index + 1);
      return isoDate(date);
    });
    return [...leading, ...dates];
  }, [anchor]);

  const upcoming = useMemo(() => {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    return Array.from({ length: 7 }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      const key = isoDate(date);
      const tone = toneFor(byDate.get(key), key, openingHours);
      return { key, day: byDate.get(key), tone, label: date.toLocaleDateString('pt-PT', { weekday: 'short', day: 'numeric' }) };
    });
  }, [byDate, openingHours]);

  function openDay(key: string) {
    if (key < today) return;
    const day = byDate.get(key) ?? {
      date: key,
      closed: false,
      dailyLimit: null,
      booked: 0,
      remaining: null,
      note: null,
      windows: [],
    };
    setEditing(day);
    setClosed(day.closed);
    setLimit(day.dailyLimit == null ? '' : String(day.dailyLimit));
    setNote(day.note ?? '');
  }

  async function save() {
    if (!editing) return;
    setSaving(true);
    try {
      await onSaveDay({ date: editing.date, closed, limit, note });
      setEditing(null);
    } finally {
      setSaving(false);
    }
  }

  const monthLabel = anchor.toLocaleDateString('pt-PT', { month: 'long', year: 'numeric' });
  const thisMonth = new Date();
  const atCurrent =
    anchor.getFullYear() === thisMonth.getFullYear() && anchor.getMonth() === thisMonth.getMonth();

  return (
    <section className="capacity-calendar" aria-label="Calendário de capacidade">
      <div className="capacity-calendar__nav">
        <Button type="button" variant="secondary" disabled={atCurrent} onClick={() => onMonth(new Date(anchor.getFullYear(), anchor.getMonth() - 1, 1))}>
          Mês anterior
        </Button>
        <strong>{monthLabel}</strong>
        <Button type="button" variant="secondary" onClick={() => onMonth(new Date(anchor.getFullYear(), anchor.getMonth() + 1, 1))}>
          Mês seguinte
        </Button>
      </div>

      <ul className="capacity-calendar__week">
        {upcoming.map((item) => (
          <li key={item.key} className={`capacity-day is-${item.tone}`}>
            <span>{item.label}</span>
            <strong>{daySummary(item.day, item.tone)}</strong>
          </li>
        ))}
      </ul>
      <ul className="capacity-legend">
        <li className="is-free">Livre</li>
        <li className="is-tight">Quase cheio</li>
        <li className="is-full">Cheio</li>
        <li className="is-closed">Fechado</li>
      </ul>

      <div className="capacity-calendar__grid" role="grid" aria-label={monthLabel}>
        {WEEKDAYS.map((label) => (
          <div key={label} className="capacity-calendar__dow" role="columnheader">
            {label}
          </div>
        ))}
        {cells.map((key, index) => {
          if (!key) return <div key={`empty-${index}`} className="capacity-day is-empty" />;
          const day = byDate.get(key);
          const tone = toneFor(day, key, openingHours);
          const past = key < today;
          return (
            <button
              key={key}
              type="button"
              className={`fh-btn capacity-day is-${tone}${past ? ' is-past' : ''}${key === today ? ' is-today' : ''}`}
              disabled={past}
              onClick={() => openDay(key)}
            >
              <span className="capacity-day__num">{Number(key.slice(-2))}</span>
              <span className="capacity-day__meta">{daySummary(day, tone)}</span>
            </button>
          );
        })}
      </div>

      <Modal
        open={editing != null}
        title={editing ? new Date(`${editing.date}T12:00:00`).toLocaleDateString('pt-PT', { weekday: 'long', day: 'numeric', month: 'long' }) : ''}
        onClose={() => setEditing(null)}
        footer={
          <Button type="button" onClick={() => void save()} disabled={saving}>
            Guardar dia
          </Button>
        }
      >
        <label className="checkbox">
          <input type="checkbox" checked={closed} onChange={(ev) => setClosed(ev.target.checked)} />
          Fechar o dia
        </label>
        {!closed && (
          <label>
            Limite de encomendas
            <input type="number" min={0} value={limit} onChange={(ev) => setLimit(ev.target.value)} />
          </label>
        )}
        <label>
          Nota
          <input value={note} onChange={(ev) => setNote(ev.target.value)} />
        </label>
      </Modal>
    </section>
  );
}
