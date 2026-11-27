/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from 'vitest';
import i18n from '../i18n';
import { createTenantSuccessMessage } from './createTenantSuccessMessage';

describe('createTenantSuccessMessage', () => {
  beforeEach(async () => {
    localStorage.setItem('floristhub.locale', 'pt-PT');
    await i18n.changeLanguage('pt-PT');
  });

  it('indica convite quando foi enviado', () => {
    const message = createTenantSuccessMessage('Flores Demo', true);
    expect(message).toBe(i18n.t('tenant.createSuccess.inviteSent', { name: 'Flores Demo' }));
  });

  it('não menciona convite quando a conta já existia', () => {
    const message = createTenantSuccessMessage('Flores Demo', false);
    expect(message).toBe(i18n.t('tenant.createSuccess.existingAccount', { name: 'Flores Demo' }));
  });
});
