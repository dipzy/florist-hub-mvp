import { describe, expect, it } from 'vitest';
import { isCatalogOrderable } from './catalogAvailability';

describe('isCatalogOrderable', () => {
  it('requires active catalog and stock', () => {
    expect(isCatalogOrderable({ isCatalogActive: true, isOutOfStock: false, stockQuantity: 1 })).toBe(true);
    expect(isCatalogOrderable({ isCatalogActive: false, isOutOfStock: false, stockQuantity: 1 })).toBe(false);
    expect(isCatalogOrderable({ isCatalogActive: true, isOutOfStock: true, stockQuantity: 0 })).toBe(false);
  });
});
