export type NewsOrder = {
  id: string;
  status: number;
  estimatedTotal: number;
  createdAt?: string | null;
};

const ACTIVE_NEWS = new Set([0, 1]);

/** Encomendas a mostrar em «Novidades» quando não há notificações por ler. */
export function recentOrderNews(orders: NewsOrder[], limit = 8): NewsOrder[] {
  const pendingOrConfirmed = orders.filter((order) => ACTIVE_NEWS.has(order.status));
  const source = pendingOrConfirmed.length > 0 ? pendingOrConfirmed : [];
  return [...source]
    .sort((a, b) => (b.createdAt ?? '').localeCompare(a.createdAt ?? ''))
    .slice(0, limit);
}
