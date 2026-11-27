import type { PublicStoreCheckout } from '../api/types';

/** Sugere zona quando o nome inclui o prefixo do código postal (ex.: «1000-123 Lisboa»). */
export function suggestDeliveryZoneId(checkout: PublicStoreCheckout | null, postalCode: string): string | undefined {
  if (!checkout || !postalCode.trim()) return undefined;
  const digits = postalCode.replace(/\D/g, '');
  if (digits.length < 4) return undefined;
  const prefix = digits.slice(0, 4);
  const active = checkout.deliveryZones.filter((z) => z.isActive);
  const byPrefix = active.find((z) => z.name.replace(/\D/g, '').includes(prefix) || z.name.includes(prefix));
  return byPrefix?.id;
}
