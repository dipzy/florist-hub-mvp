type PublicFlower = {
  isCatalogActive?: boolean;
  isOutOfStock?: boolean;
  stockQuantity?: number | null;
};

/** Flor à venda: catálogo activo e stock que não está esgotado. O flag histórico não esconde flores com stock. */
export function showUnavailableNow(flower: PublicFlower): boolean {
  if (flower.isCatalogActive === false) return true;
  if (flower.isOutOfStock === true) return true;
  if (flower.stockQuantity != null && flower.stockQuantity <= 0) return true;
  return false;
}
