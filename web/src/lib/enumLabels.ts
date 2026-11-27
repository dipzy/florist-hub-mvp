import type { TFunction } from 'i18next';
import i18n from '../i18n';

function asEnumIndex(value: number | string | null | undefined): number {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string') {
    const n = Number.parseInt(value, 10);
    if (Number.isFinite(n)) return n;
  }
  return Number.NaN;
}

/** i18next não resolve bem chaves JSON puramente numéricas; lemos o objeto do grupo. */
function labelFromGroup(_t: TFunction, groupKey: string, index: number, fallback: string): string {
  if (!Number.isFinite(index)) return fallback;
  const lng = i18n.resolvedLanguage ?? i18n.language;
  const group = i18n.getResource(lng, 'translation', groupKey) as Record<string, string> | undefined;
  const val = group?.[String(index)];
  if (typeof val === 'string' && val.length > 0) return val;
  return fallback;
}

export function orderStatusLabel(status: number | string, t: TFunction): string {
  const index = asEnumIndex(status);
  return labelFromGroup(t, 'order.status', index, String(status));
}

export function paymentStatusLabel(status: number | string, t: TFunction): string {
  const index = asEnumIndex(status);
  return labelFromGroup(t, 'order.paymentStatus', index, String(status));
}

export function invoiceKindLabel(kind: number | string, t: TFunction): string {
  const index = asEnumIndex(kind);
  return labelFromGroup(t, 'invoice.kind', index, String(kind));
}

export function invoiceStatusLabel(status: number | string, t: TFunction): string {
  const index = asEnumIndex(status);
  return labelFromGroup(t, 'invoice.status', index, String(status));
}

export function deliveryTypeLabel(deliveryType: number | string, t: TFunction): string {
  const index = asEnumIndex(deliveryType);
  return labelFromGroup(t, 'order.deliveryType', index, String(deliveryType));
}

export function arrangementBaseLabel(baseId: number | string, t: TFunction): string {
  const index = asEnumIndex(baseId);
  return labelFromGroup(t, 'configurator.base', index, String(baseId));
}

export function arrangementShapeLabel(shapeId: number | string, t: TFunction): string {
  const index = asEnumIndex(shapeId);
  return labelFromGroup(t, 'configurator.shape', index, String(shapeId));
}

const ORDER_STATUS_COUNT = 7;
const DELIVERY_TYPE_COUNT = 3;

export function orderStatusOptions(t: TFunction): string[] {
  return Array.from({ length: ORDER_STATUS_COUNT }, (_, i) => orderStatusLabel(i, t));
}

export function deliveryTypeOptions(t: TFunction): string[] {
  return Array.from({ length: DELIVERY_TYPE_COUNT }, (_, i) => deliveryTypeLabel(i, t));
}

export function paymentStatusOptions(t: TFunction): string[] {
  return Array.from({ length: 6 }, (_, i) => paymentStatusLabel(i, t));
}

export function proposalStatusLabel(status: number | string, t: TFunction): string {
  const index = asEnumIndex(status);
  return labelFromGroup(t, 'florist.proposalStatus', index, String(status));
}
