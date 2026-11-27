import { beforeEach, describe, expect, it } from 'vitest';
import i18n from '../i18n';
import pt from '../i18n/locales/pt-PT.json';
import { formatDate, formatMoney, formatNumber, getActiveLocale } from './locale';

describe('locale formatters', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('pt-PT');
  });

  it('formata dinheiro em EUR', () => {
    expect(formatMoney(12.5)).toMatch(/12[,.]50/);
  });

  it('formata números', () => {
    expect(formatNumber(1234.5)).toMatch(/1[\s.]?234/);
  });

  it('formata datas', () => {
    const formatted = formatDate(new Date('2024-03-15T12:00:00Z'), { day: '2-digit', month: 'long', year: 'numeric' });
    expect(formatted.toLowerCase()).toContain('mar');
  });

  it('resolve chaves de navegação', async () => {
    expect(pt.nav.store).toBe('Loja');
    await i18n.changeLanguage('pt-PT');
    const viaResource = i18n.getResource('pt-PT', 'translation', 'nav.store');
    expect(viaResource ?? i18n.t('nav.store')).toBe('Loja');
  });

  it('resolve locale ativo a partir do i18n', async () => {
    await i18n.changeLanguage('en');
    expect(getActiveLocale()).toBe('en');
    await i18n.changeLanguage('pt-PT');
    expect(getActiveLocale()).toBe('pt-PT');
  });
});
