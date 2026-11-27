/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from 'vitest';
import { loadCheckoutDraft, saveCheckoutDraft, clearCheckoutDraft } from './checkoutDraft';
import { setFlowerQuantity } from './basket';

describe('checkout draft basket', () => {
  beforeEach(() => {
    clearCheckoutDraft();
  });

  it('persiste o cesto após alterações de quantidade', () => {
    const flower = { id: 'f1', name: 'Girassol', unitPrice: 3, color: 'Amarelo' };
    const basket = setFlowerQuantity([], flower, 2);
    saveCheckoutDraft({
      basket,
      deliveryType: 0,
      deliveryAddress: '',
      deliveryZoneId: '',
      requestedAtIso: null,
      notes: '',
      couponCode: '',
      taxId: '',
      shape: 0,
      baseType: 1,
      variationSeed: 42,
      checkoutStep: 1,
    });
    const loaded = loadCheckoutDraft();
    expect(loaded?.basket).toEqual(basket);

    const emptied = setFlowerQuantity(basket, flower, 0);
    saveCheckoutDraft({
      ...loaded!,
      basket: emptied,
    });
    expect(loadCheckoutDraft()?.basket).toHaveLength(0);
  });
});
