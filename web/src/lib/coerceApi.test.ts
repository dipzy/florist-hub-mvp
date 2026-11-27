import { describe, expect, it } from 'vitest';
import { closedByOpeningHours, capacityFormFromDays } from './capacity';
import {
  coerceCapacityDays,
  coerceCapacitySettings,
  coerceFlower,
  coerceOpeningHours,
  coerceOrder,
  coercePaged,
  normalizeOrderStatus,
} from './coerceApi';

describe('normalizeOrderStatus', () => {
  it('aceita número, string numérica e nome do enum', () => {
    expect(normalizeOrderStatus(0)).toBe(0);
    expect(normalizeOrderStatus('1')).toBe(1);
    expect(normalizeOrderStatus('PendingConfirmation')).toBe(0);
    expect(normalizeOrderStatus('Confirmed')).toBe(1);
  });
});

describe('coercePaged', () => {
  it('lê items em camelCase, PascalCase ou uma lista crua', () => {
    const flower = { Id: 'a', Name: 'Rosa', UnitPrice: 3, ImageUrl: 'https://img/rosa.jpg', Unit: 'haste' };
    const pascal = coercePaged({ Items: [flower], Total: 12, Page: 1, PageSize: 10 }, coerceFlower);
    expect(pascal.total).toBe(12);
    expect(pascal.items[0]?.name).toBe('Rosa');
    expect(pascal.items[0]?.imageUrl).toBe('https://img/rosa.jpg');

    const list = coercePaged([{ id: 'b', name: 'Tulipa', unitPrice: 2 }], coerceFlower);
    expect(list.items).toHaveLength(1);
    expect(list.total).toBe(1);
  });

  it('normaliza encomendas dentro da página', () => {
    const page = coercePaged(
      { items: [{ id: 'o1', status: 'PendingConfirmation', estimatedTotal: 10, items: [] }] },
      (item) => coerceOrder(item as never),
    );
    expect(page.items[0]?.status).toBe(0);
  });
});

describe('coerceOrder', () => {
  it('normaliza status na encomenda', () => {
    const order = coerceOrder({
      id: 'x',
      status: '0' as unknown as number,
      baseType: 1,
      deliveryType: 0,
      estimatedTotal: 10,
      deliveryFee: 0,
      paymentCaptured: false,
      items: [],
    });
    expect(order.status).toBe(0);
  });
});

describe('capacidade', () => {
  it('lê limite 20 e janelas em PascalCase sem cair no valor inicial 8', () => {
    const settings = coerceCapacitySettings({
      DailyOrderLimit: 20,
      Windows: [
        { Label: 'Manhã', Start: '09:30', End: '13:00', OrderLimit: 10 },
        { Label: 'Tarde', Start: '14:30', End: '19:30', OrderLimit: 10 },
      ],
    });
    expect(settings.dailyOrderLimit).toBe(20);
    expect(settings.dailyOrderLimit).not.toBe(8);
    expect(settings.windows).toEqual([
      { label: 'Manhã', start: '09:30', end: '13:00', orderLimit: 10 },
      { label: 'Tarde', start: '14:30', end: '19:30', orderLimit: 10 },
    ]);

    const days = coerceCapacityDays([
      {
        Date: '2026-11-15',
        Closed: false,
        DailyLimit: 20,
        Booked: 0,
        Remaining: 20,
        Windows: [{ Label: 'Manhã', Start: '09:30', End: '13:00', Limit: 10, Booked: 0, Remaining: 10 }],
      },
    ]);
    expect(days[0]?.date).toBe('2026-11-15');
    expect(days[0]?.dailyLimit).toBe(20);
    expect(capacityFormFromDays(days).windows[0]?.orderLimit).toBe(10);

    const hours = coerceOpeningHours({ OpeningHours: [{ DayOfWeek: 0, IsClosed: true }] });
    expect(closedByOpeningHours('2026-11-15', hours)).toBe(true);
  });

  it('não rebenta se as janelas vierem em falta e preenche o formulário a partir dos dias', () => {
    expect(coerceCapacitySettings({ dailyOrderLimit: null }).windows).toEqual([]);
    expect(coerceCapacitySettings(null).dailyOrderLimit).toBeNull();
    const days = coerceCapacityDays({
      items: [
        {
          date: '2026-11-13',
          closed: false,
          dailyLimit: 20,
          booked: 2,
          remaining: 18,
          windows: [
            { label: 'Manhã', start: '09:30', end: '13:00', limit: 10, booked: 2, remaining: 8 },
            { label: 'Tarde', start: '14:30', end: '19:30', limit: 10, booked: 0, remaining: 10 },
          ],
        },
      ],
    });
    expect(capacityFormFromDays(days)).toEqual({
      dailyOrderLimit: 20,
      windows: [
        { label: 'Manhã', start: '09:30', end: '13:00', orderLimit: 10 },
        { label: 'Tarde', start: '14:30', end: '19:30', orderLimit: 10 },
      ],
    });
  });
});
