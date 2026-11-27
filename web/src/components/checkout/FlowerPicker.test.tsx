/** @vitest-environment jsdom */
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { I18nextProvider } from 'react-i18next';
import i18n, { i18nReady } from '../../i18n';
import { FlowerPicker } from './FlowerPicker';
import type { Flower } from '../../api/types';

const flowers: Flower[] = [
  {
    id: 'f1',
    name: 'Rosa',
    unitPrice: 2,
    color: 'Vermelho',
    imageUrl: null,
    stockQuantity: 10,
    isApproximatelyAvailable: true,
    isCatalogActive: true,
    category: null,
    seasonMonths: [],
    vatRate: null,
    unit: 'stem',
  },
];

describe('FlowerPicker', () => {
  it('permite − até remover a flor do cesto', async () => {
    await i18nReady;
    const onChange = vi.fn();
    const { rerender } = render(
      <I18nextProvider i18n={i18n}>
        <FlowerPicker flowers={flowers} basket={[{ flowerId: 'f1', name: 'Rosa', quantity: 1, unitPrice: 2 }]} onChange={onChange} />
      </I18nextProvider>,
    );

    fireEvent.click(screen.getAllByLabelText(/Diminuir quantidade/i)[0]);
    expect(onChange).toHaveBeenCalledWith([]);

    onChange.mockClear();
    rerender(
      <I18nextProvider i18n={i18n}>
        <FlowerPicker flowers={flowers} basket={[]} onChange={onChange} />
      </I18nextProvider>,
    );
    const minus = screen.getByLabelText(/Diminuir quantidade/i);
    expect((minus as HTMLButtonElement).disabled).toBe(true);
  });
});
