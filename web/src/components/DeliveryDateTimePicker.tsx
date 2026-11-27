import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { CapacityDay, OpeningHour } from '../api/types';
import { dayIsUnavailable, slotHasCapacity } from '../lib/capacity';
import {
  DELIVERY_TIME_SLOT_MINUTES,
  formatRequestedDeliveryPt,
  selectableDeliveryDates,
  timeSlotsForDate,
} from '../lib/deliveryScheduling';
import { formatDate } from '../lib/locale';
import { InlineSelect } from './ui/InlineSelect';

type Props = {
  value: Date | null;
  onChange: (value: Date | null) => void;
  openingHours: OpeningHour[];
  minimumAdvanceMinutes: number;
  disabled?: boolean;
  capacityDays?: CapacityDay[];
};

export function DeliveryDateTimePicker({
  value,
  onChange,
  openingHours,
  minimumAdvanceMinutes,
  disabled,
  capacityDays = [],
}: Props) {
  const { t } = useTranslation();
  const dates = useMemo(() => selectableDeliveryDates(openingHours), [openingHours]);
  const [dateIso, setDateIso] = useState(() => {
    if (value) {
      const y = value.getFullYear();
      const m = String(value.getMonth() + 1).padStart(2, '0');
      const d = String(value.getDate()).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }
    return dates[0] ?? '';
  });

  const capacityFor = (iso: string) => capacityDays.find((day) => day.date === iso);
  const [capacityWindow, setCapacityWindow] = useState('');
  const dayCapacity = dateIso ? capacityFor(dateIso) : undefined;
  const deliveryWindows = useMemo(() => {
    if (!dayCapacity?.windows.length) return [];
    return dayCapacity.windows.filter((w) => w.remaining > 0);
  }, [dayCapacity]);

  useEffect(() => {
    if (deliveryWindows.length === 1 && !capacityWindow) {
      setCapacityWindow(deliveryWindows[0]!.label);
    }
  }, [deliveryWindows, capacityWindow]);

  const activeWindow = useMemo(() => {
    if (deliveryWindows.length === 0) return undefined;
    if (deliveryWindows.length === 1) return deliveryWindows[0];
    return deliveryWindows.find((w) => w.label === capacityWindow);
  }, [deliveryWindows, capacityWindow]);

  const slots = useMemo(() => {
    const base = dateIso ? timeSlotsForDate(dateIso, openingHours, minimumAdvanceMinutes) : [];
    const scoped = activeWindow
      ? base.filter((slot) => {
          const [sh, sm] = activeWindow.start.split(':').map(Number);
          const [eh, em] = activeWindow.end.split(':').map(Number);
          const [th, tm] = slot.label.split(':').map(Number);
          const t = th * 60 + tm;
          return t >= sh * 60 + sm && t <= eh * 60 + em;
        })
      : base;
    return scoped.filter((slot) => slotHasCapacity(slot.label, capacityFor(dateIso)));
  }, [dateIso, openingHours, minimumAdvanceMinutes, capacityDays, activeWindow]);

  const slotsIgnoringAdvance = useMemo(() => {
    if (!dateIso) return [];
    const base = timeSlotsForDate(dateIso, openingHours, 0);
    const scoped = activeWindow
      ? base.filter((slot) => {
          const [sh, sm] = activeWindow.start.split(':').map(Number);
          const [eh, em] = activeWindow.end.split(':').map(Number);
          const [th, tm] = slot.label.split(':').map(Number);
          const tmins = th * 60 + tm;
          return tmins >= sh * 60 + sm && tmins <= eh * 60 + em;
        })
      : base;
    return scoped.filter((slot) => slotHasCapacity(slot.label, capacityFor(dateIso)));
  }, [dateIso, openingHours, activeWindow, capacityDays]);

  const noSlotsMessage = useMemo(() => {
    if (!dateIso || slots.length > 0) return null;
    if (deliveryWindows.length > 0 && slotsIgnoringAdvance.length > 0) {
      return t('delivery.picker.noSlotsAdvance');
    }
    if (dayCapacity?.windows.length && deliveryWindows.length === 0 && !dayCapacity.closed) {
      return t('delivery.picker.noSlotsWindows');
    }
    return t('delivery.picker.noSlotsDefault');
  }, [dateIso, slots.length, deliveryWindows.length, slotsIgnoringAdvance.length, dayCapacity, t]);

  const timeValue = value ? `${String(value.getHours()).padStart(2, '0')}:${String(value.getMinutes()).padStart(2, '0')}` : '';

  return (
    <fieldset className="delivery-datetime" disabled={disabled}>
      <legend>{t('delivery.picker.legend')}</legend>
      {value && (
        <p className="muted">
          {t('delivery.picker.selected', { value: formatRequestedDeliveryPt(value) })}
        </p>
      )}
      <label>
        {t('delivery.picker.date')}
        <InlineSelect
          value={dateIso}
          onChange={(ev) => {
            setDateIso(ev.target.value);
            setCapacityWindow('');
            onChange(null);
          }}
        >
          {dates.length === 0 && <option value="">{t('delivery.picker.noDates')}</option>}
          {dates.map((d) => {
            const full = dayIsUnavailable(capacityFor(d));
            return (
              <option key={d} value={d} disabled={full}>
                {formatDate(new Date(d + 'T12:00:00'), { weekday: 'short', day: '2-digit', month: 'short' })}
                {full ? ` — ${t('common.noSlot')}` : ''}
              </option>
            );
          })}
        </InlineSelect>
      </label>
      {deliveryWindows.length > 1 && (
        <label>
          {t('delivery.picker.window')}
          <InlineSelect
            value={capacityWindow}
            onChange={(ev) => {
              setCapacityWindow(ev.target.value);
              onChange(null);
            }}
          >
            <option value="">{t('delivery.picker.selectPlaceholder')}</option>
            {deliveryWindows.map((w) => (
              <option key={w.label} value={w.label}>
                {t('delivery.picker.windowOption', {
                  label: w.label,
                  start: w.start,
                  end: w.end,
                  remaining: w.remaining,
                })}
              </option>
            ))}
          </InlineSelect>
        </label>
      )}
      <label>
        {t('delivery.picker.time', { minutes: DELIVERY_TIME_SLOT_MINUTES })}
        <InlineSelect
          value={timeValue}
          onChange={(ev) => {
            const slot = slots.find((s) => s.label === ev.target.value);
            onChange(slot ? slot.date : null);
          }}
          disabled={slots.length === 0 || (deliveryWindows.length > 1 && !capacityWindow) || !dateIso}
        >
          <option value="">{t('delivery.picker.selectPlaceholder')}</option>
          {slots.map((s) => (
            <option key={s.label} value={s.label}>
              {s.label}
            </option>
          ))}
        </InlineSelect>
      </label>
      {noSlotsMessage && <p className="muted">{noSlotsMessage}</p>}
    </fieldset>
  );
}
