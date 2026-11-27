import { describe, expect, it } from 'vitest';
import i18n, { i18nReady } from '../i18n';
import { deliveryTypeLabel, orderStatusLabel } from './enumLabels';

describe('enumLabels', () => {
  it('traduz estados numéricos e em string', async () => {
    await i18nReady;
    await i18n.changeLanguage('pt-PT');
    const t = i18n.getFixedT('pt-PT');
    expect(orderStatusLabel(0, t)).toBe('Pendente de confirmação');
    expect(orderStatusLabel('1', t)).toBe('Confirmada');
    expect(deliveryTypeLabel('2', t)).toMatch(/Cemitério|local/i);
  });
});
