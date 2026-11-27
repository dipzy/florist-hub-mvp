import type { Flower } from '../api/types';
import type { StemInput } from './layout';

export const DEMO_FLOWERS: Flower[] = [
  { id: 'demo-rosa', name: 'Rosa vermelha', color: 'Vermelho', unitPrice: 2.5, isApproximatelyAvailable: true },
  { id: 'demo-tulipa', name: 'Túlipa amarela', color: 'Amarelo', unitPrice: 1.8, isApproximatelyAvailable: true },
  { id: 'demo-girassol', name: 'Girassol', color: 'Amarelo', unitPrice: 2.2, isApproximatelyAvailable: true },
  { id: 'demo-margarida', name: 'Margarida silvestre', color: 'Branco', unitPrice: 1.4, isApproximatelyAvailable: true },
  { id: 'demo-lirio', name: 'Lírio', color: 'Branco', unitPrice: 2.4, isApproximatelyAvailable: true },
  { id: 'demo-eucalipto', name: 'Eucalipto', color: 'Verde', unitPrice: 0.9, isApproximatelyAvailable: true },
];

export const DEMO_BASKET: StemInput[] = [
  { name: 'Rosa vermelha', quantity: 3, color: 'Vermelho' },
  { name: 'Túlipa amarela', quantity: 2, color: 'Amarelo' },
  { name: 'Girassol', quantity: 1, color: 'Amarelo' },
  { name: 'Margarida silvestre', quantity: 2, color: 'Branco' },
  { name: 'Lírio', quantity: 2, color: 'Branco' },
];
