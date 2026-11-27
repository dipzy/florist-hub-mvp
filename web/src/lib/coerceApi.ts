import type {
  CapacityDay,
  CapacitySettings,
  CapacityWindow,
  CapacityWindowInput,
  CatalogFlower,
  OpeningHour,
  Order,
  Paged,
} from '../api/types';

const ORDER_STATUS_BY_NAME: Record<string, number> = {
  PendingConfirmation: 0,
  Confirmed: 1,
  InPreparation: 2,
  Ready: 3,
  Delivered: 4,
  Cancelled: 5,
  Rejected: 6,
};

export function normalizeOrderStatus(status: unknown): number {
  if (typeof status === 'number' && Number.isFinite(status)) return status;
  if (typeof status === 'string') {
    const n = Number.parseInt(status, 10);
    if (Number.isFinite(n)) return n;
    const named = ORDER_STATUS_BY_NAME[status];
    if (named != null) return named;
  }
  return Number.NaN;
}

export function coerceOrder(raw: Order): Order {
  const status = normalizeOrderStatus(raw.status);
  return Number.isNaN(status) ? raw : { ...raw, status };
}

export function coerceOrders(list: Order[] | null | undefined): Order[] {
  if (!Array.isArray(list)) return [];
  return list.map(coerceOrder);
}

function readField(raw: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    if (raw[key] != null) return raw[key];
  }
  return undefined;
}

export function coerceFlower(raw: unknown): CatalogFlower {
  const row = (raw ?? {}) as Record<string, unknown>;
  const season = readField(row, 'seasonMonths', 'SeasonMonths');
  return {
    id: String(readField(row, 'id', 'Id') ?? ''),
    name: String(readField(row, 'name', 'Name') ?? ''),
    color: (readField(row, 'color', 'Color') as string | undefined) ?? undefined,
    unitPrice: Number(readField(row, 'unitPrice', 'UnitPrice') ?? 0),
    unit: String(readField(row, 'unit', 'Unit') ?? 'unidade'),
    stockQuantity: (readField(row, 'stockQuantity', 'StockQuantity') as number | null | undefined) ?? null,
    minimumStockQuantity: (readField(row, 'minimumStockQuantity', 'MinimumStockQuantity') as number | null | undefined) ?? null,
    isOutOfStock: Boolean(readField(row, 'isOutOfStock', 'IsOutOfStock')),
    isLowStock: Boolean(readField(row, 'isLowStock', 'IsLowStock')),
    isApproximatelyAvailable: readField(row, 'isApproximatelyAvailable', 'IsApproximatelyAvailable') !== false,
    isCatalogActive: readField(row, 'isCatalogActive', 'IsCatalogActive') !== false,
    imageUrl: (readField(row, 'imageUrl', 'ImageUrl') as string | undefined) || undefined,
    notes: (readField(row, 'notes', 'Notes') as string | undefined) ?? undefined,
    vatRate: (readField(row, 'vatRate', 'VatRate') as number | null | undefined) ?? null,
    category: (readField(row, 'category', 'Category') as string | null | undefined) ?? null,
    seasonMonths: Array.isArray(season) ? season.map(Number) : [],
  };
}

/** Aceita lista crua ou página com `items`/`Items` (camelCase ou PascalCase). */
export function coercePaged<T>(raw: unknown, mapItem: (item: unknown) => T): Paged<T> {
  if (Array.isArray(raw)) {
    const items = raw.map(mapItem);
    return { items, total: items.length, page: 1, pageSize: Math.max(1, items.length) };
  }
  if (!raw || typeof raw !== 'object') {
    return { items: [], total: 0, page: 1, pageSize: 1 };
  }
  const row = raw as Record<string, unknown>;
  const list = readField(row, 'items', 'Items', 'data', 'Data');
  const items = Array.isArray(list) ? list.map(mapItem) : [];
  const totalRaw = Number(readField(row, 'total', 'Total', 'totalCount', 'TotalCount') ?? items.length);
  const pageRaw = Number(readField(row, 'page', 'Page') ?? 1);
  const sizeRaw = Number(readField(row, 'pageSize', 'PageSize') ?? Math.max(1, items.length));
  return {
    items,
    total: Number.isFinite(totalRaw) ? totalRaw : items.length,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: Number.isFinite(sizeRaw) && sizeRaw > 0 ? sizeRaw : Math.max(1, items.length),
  };
}

function asRecord(raw: unknown): Record<string, unknown> {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return {};
  return raw as Record<string, unknown>;
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === '') return null;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function clock(value: unknown): string {
  const text = String(value ?? '');
  const match = text.match(/^(\d{2}:\d{2})/);
  return match?.[1] ?? text;
}

function dateKey(value: unknown): string {
  const match = String(value ?? '').match(/^(\d{4}-\d{2}-\d{2})/);
  return match?.[1] ?? '';
}

function coerceWindowInput(raw: unknown): CapacityWindowInput {
  const row = asRecord(raw);
  return {
    label: String(readField(row, 'label', 'Label') ?? ''),
    start: clock(readField(row, 'start', 'Start')),
    end: clock(readField(row, 'end', 'End')),
    orderLimit: numberOrNull(readField(row, 'orderLimit', 'OrderLimit', 'limit', 'Limit')) ?? 0,
  };
}

function coerceWindow(raw: unknown): CapacityWindow {
  const row = asRecord(raw);
  const input = coerceWindowInput(raw);
  return {
    label: input.label,
    start: input.start,
    end: input.end,
    limit: input.orderLimit,
    booked: numberOrNull(readField(row, 'booked', 'Booked')) ?? 0,
    remaining: numberOrNull(readField(row, 'remaining', 'Remaining')) ?? 0,
  };
}

/** Limite e janelas, em camelCase ou PascalCase, sem inventar um limite. */
export function coerceCapacitySettings(raw: unknown): CapacitySettings {
  const row = asRecord(raw);
  const windowsRaw = readField(row, 'windows', 'Windows');
  const exceptionsRaw = readField(row, 'exceptions', 'Exceptions');
  const exceptions = Array.isArray(exceptionsRaw) ? exceptionsRaw : [];
  return {
    dailyOrderLimit: numberOrNull(readField(row, 'dailyOrderLimit', 'DailyOrderLimit')),
    windows: Array.isArray(windowsRaw) ? windowsRaw.map(coerceWindowInput) : [],
    exceptions: exceptions.map((item) => {
      const exception = asRecord(item);
      return {
        date: dateKey(readField(exception, 'date', 'Date')),
        isClosed: Boolean(readField(exception, 'isClosed', 'IsClosed')),
        dailyOrderLimitOverride: numberOrNull(readField(exception, 'dailyOrderLimitOverride', 'DailyOrderLimitOverride')),
        note: (readField(exception, 'note', 'Note') as string | null | undefined) ?? null,
      };
    }),
  };
}

/** Dias do calendário. Aceita lista, página, ou `limit` no lugar de `orderLimit`. */
export function coerceCapacityDays(raw: unknown): CapacityDay[] {
  const list = Array.isArray(raw) ? raw : readField(asRecord(raw), 'items', 'Items', 'days', 'Days');
  if (!Array.isArray(list)) return [];
  return list.flatMap((item) => {
    const row = asRecord(item);
    const date = dateKey(readField(row, 'date', 'Date'));
    if (!date) return [];
    const windowsRaw = readField(row, 'windows', 'Windows');
    return [
      {
        date,
        closed: Boolean(readField(row, 'closed', 'Closed')),
        dailyLimit: numberOrNull(readField(row, 'dailyLimit', 'DailyLimit')),
        booked: numberOrNull(readField(row, 'booked', 'Booked')) ?? 0,
        remaining: numberOrNull(readField(row, 'remaining', 'Remaining')),
        note: (readField(row, 'note', 'Note') as string | null | undefined) ?? null,
        windows: Array.isArray(windowsRaw) ? windowsRaw.map(coerceWindow) : [],
      },
    ];
  });
}

export function coerceOpeningHours(raw: unknown): OpeningHour[] {
  const list = Array.isArray(raw) ? raw : readField(asRecord(raw), 'openingHours', 'OpeningHours');
  if (!Array.isArray(list)) return [];
  return list.flatMap((item) => {
    const row = asRecord(item);
    const dayOfWeek = numberOrNull(readField(row, 'dayOfWeek', 'DayOfWeek'));
    if (dayOfWeek == null) return [];
    const openTime = readField(row, 'openTime', 'OpenTime');
    const closeTime = readField(row, 'closeTime', 'CloseTime');
    return [
      {
        dayOfWeek,
        isClosed: Boolean(readField(row, 'isClosed', 'IsClosed')),
        openTime: typeof openTime === 'string' ? openTime : undefined,
        closeTime: typeof closeTime === 'string' ? closeTime : undefined,
      },
    ];
  });
}
