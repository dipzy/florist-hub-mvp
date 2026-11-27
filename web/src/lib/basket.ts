export type BasketLine = {
  flowerId?: string;
  name: string;
  quantity: number;
  unitPrice: number;
  color?: string;
};

export function basketLineKey(line: BasketLine): string {
  return line.flowerId ?? line.name;
}

export function basketItemsTotal(basket: BasketLine[]): number {
  return basket.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
}

export function quantityForFlowerId(basket: BasketLine[], flowerId: string): number {
  return basket.find((line) => line.flowerId === flowerId)?.quantity ?? 0;
}

export function setFlowerQuantity(
  basket: BasketLine[],
  flower: { id: string; name: string; unitPrice: number; color?: string | null },
  quantity: number,
): BasketLine[] {
  const nextQty = Math.max(0, Math.floor(quantity));
  if (nextQty === 0) {
    return basket.filter((line) => line.flowerId !== flower.id);
  }
  const existing = basket.find((line) => line.flowerId === flower.id);
  if (existing) {
    return basket.map((line) => (line.flowerId === flower.id ? { ...line, quantity: nextQty } : line));
  }
  return [
    ...basket,
    {
      flowerId: flower.id,
      name: flower.name,
      quantity: nextQty,
      unitPrice: flower.unitPrice,
      color: flower.color ?? undefined,
    },
  ];
}

export function adjustLineQuantity(basket: BasketLine[], lineKey: string, delta: number): BasketLine[] {
  const idx = basket.findIndex((line) => basketLineKey(line) === lineKey);
  if (idx < 0) return basket;
  const line = basket[idx];
  const nextQty = line.quantity + delta;
  if (nextQty <= 0) {
    return basket.filter((_, i) => i !== idx);
  }
  return basket.map((item, i) => (i === idx ? { ...item, quantity: nextQty } : item));
}

export function removeBasketLine(basket: BasketLine[], lineKey: string): BasketLine[] {
  return basket.filter((line) => basketLineKey(line) !== lineKey);
}

export function clearBasketLines(): BasketLine[] {
  return [];
}
