import type { TFunction } from 'i18next';
import type { AuthContextOption } from '../api/types';

export function sessionContextLabel(option: AuthContextOption, t: TFunction): string {
  if (option.kind === 'SuperAdmin') return t('session.superAdminPlatform');
  return option.tenantName ?? t('session.storeFallback');
}

export function isActiveSessionContext(
  option: AuthContextOption,
  role: string | undefined,
  tenantId: string | undefined,
): boolean {
  if (option.kind === 'SuperAdmin') return role === 'SuperAdmin';
  return role === 'Florista' && option.tenantId != null && option.tenantId === tenantId;
}

export function shouldShowSessionSwitcher(contexts: AuthContextOption[] | undefined): boolean {
  return (contexts?.length ?? 0) > 1;
}
