import type { CatalogFlower, Flower } from '../api/types';
import { isFlowerExhausted } from './capacity';

export function isCatalogOrderable(flower: Pick<Flower, 'isCatalogActive' | 'isOutOfStock' | 'stockQuantity'>): boolean {
  const active = flower.isCatalogActive !== false;
  return active && !isFlowerExhausted(flower);
}

export function catalogOrderableLabel(flower: CatalogFlower, yes: string, no: string): string {
  return isCatalogOrderable(flower) ? yes : no;
}
