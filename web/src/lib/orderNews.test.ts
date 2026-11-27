import { describe, expect, it } from 'vitest';
import { recentOrderNews } from './orderNews';

describe('recentOrderNews', () => {
  it('lista pendentes e confirmadas mesmo sem notificações', () => {
    const news = recentOrderNews([
      { id: 'a', status: 4, estimatedTotal: 10, createdAt: '2026-11-12T10:00:00Z' },
      { id: 'b', status: 0, estimatedTotal: 20, createdAt: '2026-11-11T10:00:00Z' },
      { id: 'c', status: 1, estimatedTotal: 30, createdAt: '2026-11-12T09:00:00Z' },
    ]);
    expect(news.map((order) => order.id)).toEqual(['c', 'b']);
  });

  it('fica vazio quando não há encomendas activas', () => {
    expect(recentOrderNews([])).toEqual([]);
    expect(recentOrderNews([{ id: 'd', status: 4, estimatedTotal: 1 }])).toEqual([]);
  });
});
