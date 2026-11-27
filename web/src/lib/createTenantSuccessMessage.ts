import i18n from '../i18n';

export function createTenantSuccessMessage(tenantName: string, floristInviteSent: boolean): string {
  if (floristInviteSent) {
    return i18n.t('tenant.createSuccess.inviteSent', { name: tenantName });
  }
  return i18n.t('tenant.createSuccess.existingAccount', { name: tenantName });
}
