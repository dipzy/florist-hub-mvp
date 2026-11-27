import { describe, expect, it } from 'vitest';
import { showUnavailableNow } from './publicFlowerAvailability';

describe('showUnavailableNow', () => {
  it('não marca como indisponível uma flor com stock, mesmo com o flag antigo a false', () => {
    expect(
      showUnavailableNow({
        isCatalogActive: true,
        isOutOfStock: false,
        stockQuantity: 69,
      }),
    ).toBe(false);
  });

  it('marca esgotado e inactivo', () => {
    expect(showUnavailableNow({ isCatalogActive: true, isOutOfStock: true, stockQuantity: 0 })).toBe(true);
    expect(showUnavailableNow({ isCatalogActive: false, isOutOfStock: false, stockQuantity: 10 })).toBe(true);
  });
});
