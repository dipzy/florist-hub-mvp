import type { OpeningHour } from '../api/types';
import { getIntlLocale } from './locale';

export const DELIVERY_TIME_SLOT_MINUTES = 15;

export type DeliveryTimeSlot = {
  label: string;
  value: string;
  date: Date;
};

function parseTime(t: string | undefined): number | null {
  if (!t) return null;
  const [h, m] = t.split(':').map(Number);
  if (Number.isNaN(h) || Number.isNaN(m)) return null;
  return h * 60 + m;
}

function openingHourForDay(day: number, openingHours: OpeningHour[]): OpeningHour | undefined {
  return openingHours.find((h) => h.dayOfWeek === day);
}

export function isStoreClosedOnDay(day: number, openingHours: OpeningHour[]): boolean {
  if (openingHours.length === 0) return false;
  const config = openingHourForDay(day, openingHours);
  if (!config) return false;
  return config.isClosed;
}

function formatLocalDateIso(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function selectableDeliveryDates(openingHours: OpeningHour[], daysAhead = 60): string[] {
  const out: string[] = [];
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  for (let i = 0; i < daysAhead; i++) {
    const d = new Date(start);
    d.setDate(start.getDate() + i);
    if (!isStoreClosedOnDay(d.getDay(), openingHours)) {
      out.push(formatLocalDateIso(d));
    }
  }
  return out;
}

export function timeSlotsForDate(
  dateIso: string,
  openingHours: OpeningHour[],
  minimumAdvanceMinutes: number,
  slotMinutes = DELIVERY_TIME_SLOT_MINUTES,
): DeliveryTimeSlot[] {
  const config = openingHourForDay(new Date(dateIso + 'T12:00:00').getDay(), openingHours);
  const open = parseTime(config?.openTime) ?? 9 * 60;
  const close = parseTime(config?.closeTime) ?? 18 * 60;
  const minWhen = Date.now() + minimumAdvanceMinutes * 60_000;

  const slots: DeliveryTimeSlot[] = [];
  for (let minuteOfDay = open; minuteOfDay <= close; minuteOfDay += slotMinutes) {
    const h = Math.floor(minuteOfDay / 60);
    const m = minuteOfDay % 60;
    const d = new Date(dateIso + 'T00:00:00');
    d.setHours(h, m, 0, 0);
    if (d.getTime() >= minWhen) {
      const label = `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
      slots.push({ label, value: `${dateIso}T${label}`, date: d });
    }
  }
  return slots;
}

export function formatRequestedDeliveryPt(date: Date): string {
  return date.toLocaleString(getIntlLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function needsDeliveryZone(deliveryType: number, zoneCount: number): boolean {
  return zoneCount > 0 && (deliveryType === 1 || deliveryType === 2);
}
