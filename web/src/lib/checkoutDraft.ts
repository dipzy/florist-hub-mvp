const STORAGE_KEY = 'floristhub.checkoutDraft';

export type CheckoutDraft = {
  basket: { flowerId?: string; name: string; quantity: number; unitPrice: number; color?: string }[];
  deliveryType: number | '';
  deliveryAddress: string;
  deliveryZoneId: string;
  requestedAtIso: string | null;
  notes: string;
  couponCode: string;
  taxId: string;
  shape: number;
  baseType: number;
  variationSeed: number;
  inspireId?: string;
  checkoutStep?: 1 | 2 | 3;
  postalCode?: string;
};

export function saveCheckoutDraft(draft: CheckoutDraft) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(draft));
  } catch {
    /* quota / privado */
  }
}

export function loadCheckoutDraft(): CheckoutDraft | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as CheckoutDraft;
  } catch {
    return null;
  }
}

export function clearCheckoutDraft() {
  sessionStorage.removeItem(STORAGE_KEY);
}
