import type { CapacityDay, CapacityWindowInput, OpeningHour } from '../api/types';
import { isStoreClosedOnDay } from './deliveryScheduling';

export type { CapacityDay };

export function isFlowerExhausted(flower: { stockQuantity?: number | null; isOutOfStock?: boolean }): boolean {
  if (flower.isOutOfStock) return true;
  return flower.stockQuantity != null && flower.stockQuantity <= 0;
}

export function dayIsUnavailable(day: CapacityDay | undefined): boolean {
  if (!day) return false;
  if (day.closed) return true;
  if (day.remaining === 0) return true;
  if (day.windows.length > 0 && day.windows.every((window) => window.remaining <= 0)) return true;
  return false;
}

function minutes(label: string): number | null {
  const [hour, minute] = label.split(':').map(Number);
  if (Number.isNaN(hour) || Number.isNaN(minute)) return null;
  return hour * 60 + minute;
}

export type CapacityTone = 'free' | 'tight' | 'full' | 'closed' | 'unknown';

export function capacityDayKey(value: string | null | undefined): string {
  const match = String(value ?? '').match(/^(\d{4}-\d{2}-\d{2})/);
  return match?.[1] ?? '';
}

export function capacityTone(
  day: CapacityDay | undefined,
  options?: { closedByHours?: boolean },
): CapacityTone {
  if (options?.closedByHours || day?.closed) return 'closed';
  if (!day) return 'unknown';
  if (day.remaining === 0) return 'full';
  if (day.dailyLimit == null || day.dailyLimit <= 0) return day.booked > 0 ? 'full' : 'free';
  const ratio = day.booked / day.dailyLimit;
  if (ratio >= 1) return 'full';
  if (ratio >= 0.7) return 'tight';
  return 'free';
}

export function closedByOpeningHours(dateIso: string, openingHours: OpeningHour[]): boolean {
  const key = capacityDayKey(dateIso);
  if (!key) return false;
  const day = new Date(`${key}T12:00:00`).getDay();
  return isStoreClosedOnDay(day, openingHours);
}

/** Limite e janelas visíveis no calendário, quando as definições não trazem esses campos. */
export function capacityFormFromDays(days: CapacityDay[]): {
  dailyOrderLimit: number | null;
  windows: CapacityWindowInput[];
} {
  const withLimit = days.find((day) => day.dailyLimit != null);
  const withWindows = days.find((day) => day.windows.length > 0);
  return {
    dailyOrderLimit: withLimit?.dailyLimit ?? null,
    windows: (withWindows?.windows ?? []).map((window) => ({
      label: window.label,
      start: window.start,
      end: window.end,
      orderLimit: window.limit,
    })),
  };
}

export function slotHasCapacity(label: string, day: CapacityDay | undefined): boolean {
  if (!day) return true;
  if (day.closed || day.remaining === 0) return false;
  if (day.windows.length === 0) return true;
  const minute = minutes(label);
  if (minute == null) return false;
  const window = day.windows.find((candidate) => {
    const start = minutes(candidate.start);
    const end = minutes(candidate.end);
    return start != null && end != null && minute >= start && minute <= end;
  });
  return !!window && window.remaining > 0;
}
