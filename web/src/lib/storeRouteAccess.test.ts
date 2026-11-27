/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { buildArrangementSubmitHint, canSubmitStoreOrder } from './storeRouteAccess';

describe('storeRouteAccess', () => {
  beforeEach(async () => {
    localStorage.setItem('floristhub.locale', 'pt-PT');
    await i18n.changeLanguage('pt-PT');
  });

  it('permite submissão apenas a clientes autenticados', () => {
    expect(canSubmitStoreOrder(false)).toBe(false);
    expect(canSubmitStoreOrder(true, 'Florista')).toBe(false);
    expect(canSubmitStoreOrder(true, 'SuperAdmin')).toBe(false);
    expect(canSubmitStoreOrder(true, 'Cliente')).toBe(true);
  });

  it('explica o login de cliente para visitantes e papéis de gestão', () => {
    expect(buildArrangementSubmitHint(false)).toBe(i18n.t('checkout.submitHint.guest'));
    expect(buildArrangementSubmitHint(true, 'Florista')).toBe(i18n.t('checkout.submitHint.wrongRole'));
    expect(buildArrangementSubmitHint(true, 'Cliente')).toBeNull();
  });
});
