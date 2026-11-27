import i18n from '../i18n';

/** A rota «Montar arranjo» é pública; só a submissão exige conta Cliente. */
export function canSubmitStoreOrder(isAuthenticated: boolean, role?: string): boolean {
  return isAuthenticated && role === 'Cliente';
}

export function buildArrangementSubmitHint(isAuthenticated: boolean, role?: string): string | null {
  if (canSubmitStoreOrder(isAuthenticated, role)) return null;
  if (!isAuthenticated) {
    return i18n.t('checkout.submitHint.guest');
  }
  if (role === 'Florista' || role === 'SuperAdmin') {
    return i18n.t('checkout.submitHint.wrongRole');
  }
  return i18n.t('checkout.submitHint.guest');
}
