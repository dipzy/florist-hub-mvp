import { describe, expect, it } from 'vitest';
import {
  adjustLineQuantity,
  basketItemsTotal,
  clearBasketLines,
  removeBasketLine,
  setFlowerQuantity,
} from './basket';

const rose = { id: 'r1', name: 'Rosa', unitPrice: 2.5, color: 'Vermelho' };
const tulip = { id: 't1', name: 'Túlipa', unitPrice: 1.2, color: 'Amarelo' };

describe('basket helpers', () => {
  it('adiciona e incrementa quantidades', () => {
    let basket = setFlowerQuantity([], rose, 1);
    expect(basket).toHaveLength(1);
    expect(basket[0].quantity).toBe(1);
    basket = setFlowerQuantity(basket, rose, 3);
    expect(basket[0].quantity).toBe(3);
    basket = setFlowerQuantity(basket, tulip, 2);
    expect(basket).toHaveLength(2);
    expect(basketItemsTotal(basket)).toBeCloseTo(2.5 * 3 + 1.2 * 2, 5);
  });

  it('quantidade 0 remove a flor do cesto', () => {
    const basket = setFlowerQuantity(setFlowerQuantity([], rose, 2), rose, 0);
    expect(basket).toHaveLength(0);
  });

  it('adjustLineQuantity com −1 remove linha ao chegar a 0', () => {
    const start = setFlowerQuantity([], rose, 1);
    const next = adjustLineQuantity(start, 'r1', -1);
    expect(next).toHaveLength(0);
  });

  it('removeBasketLine e clearBasketLines', () => {
    const start = setFlowerQuantity(setFlowerQuantity([], rose, 2), tulip, 1);
    expect(removeBasketLine(start, 'r1')).toHaveLength(1);
    expect(clearBasketLines()).toEqual([]);
  });
});
