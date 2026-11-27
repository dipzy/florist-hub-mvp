import { describe, expect, it } from 'vitest';
import {
  capacityDayKey,
  capacityTone,
  closedByOpeningHours,
  dayIsUnavailable,
  isFlowerExhausted,
  slotHasCapacity,
  type CapacityDay,
} from './capacity';

const openDay: CapacityDay = {
  date: '2026-11-20',
  closed: false,
  dailyLimit: 4,
  booked: 1,
  remaining: 3,
  windows: [
    { label: 'Manhã', start: '09:00', end: '11:59', limit: 2, booked: 2, remaining: 0 },
    { label: 'Tarde', start: '12:00', end: '19:00', limit: 2, booked: 0, remaining: 2 },
  ],
};

describe('capacidade e stock', () => {
  it('marca a flor esgotada quando o stock controlado chega a zero', () => {
    expect(isFlowerExhausted({ stockQuantity: 0 })).toBe(true);
    expect(isFlowerExhausted({ isOutOfStock: true })).toBe(true);
    expect(isFlowerExhausted({ stockQuantity: null })).toBe(false);
    expect(isFlowerExhausted({})).toBe(false);
  });

  it('trata o dia cheio ou fechado como indisponível', () => {
    expect(dayIsUnavailable(undefined)).toBe(false);
    expect(dayIsUnavailable({ ...openDay, remaining: 0, windows: [] })).toBe(true);
    expect(dayIsUnavailable({ ...openDay, closed: true })).toBe(true);
  });

  it('normaliza a data e distingue fechado, livre, cheio e desconhecido', () => {
    expect(capacityDayKey('2026-11-12T00:00:00')).toBe('2026-11-12');
    expect(capacityTone(undefined)).toBe('unknown');
    expect(capacityTone(undefined, { closedByHours: true })).toBe('closed');
    expect(capacityTone({ ...openDay, closed: true })).toBe('closed');
    expect(capacityTone({ ...openDay, booked: 0, remaining: 4, dailyLimit: 4, windows: [] })).toBe('free');
    expect(capacityTone({ ...openDay, booked: 3, remaining: 1, dailyLimit: 4, windows: [] })).toBe('tight');
    expect(capacityTone({ ...openDay, booked: 4, remaining: 0, dailyLimit: 4, windows: [] })).toBe('full');
  });

  it('trata o domingo fechado no checkout como indisponível', () => {
    expect(
      closedByOpeningHours('2026-11-15', [{ dayOfWeek: 0, isClosed: true, openTime: null, closeTime: null }]),
    ).toBe(true);
    expect(
      closedByOpeningHours('2026-11-16', [{ dayOfWeek: 0, isClosed: true, openTime: null, closeTime: null }]),
    ).toBe(false);
  });

  it('só aceita horários dentro de uma janela com vaga', () => {
    expect(slotHasCapacity('10:00', openDay)).toBe(false);
    expect(slotHasCapacity('15:00', openDay)).toBe(true);
    expect(slotHasCapacity('08:00', openDay)).toBe(false);
    expect(slotHasCapacity('15:00', undefined)).toBe(true);
  });
});
