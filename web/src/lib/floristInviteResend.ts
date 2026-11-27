import type { PlatformTenantOnboarding } from '../api/types';

/** Convite só faz sentido enquanto a florista ainda não concluiu o primeiro acesso. */
export function shouldShowFloristInviteResend(onboarding: PlatformTenantOnboarding | null | undefined): boolean {
  if (!onboarding) return false;
  return !onboarding.floristInviteCompleted;
}

export function floristInviteResendUnavailableMessage(onboarding: PlatformTenantOnboarding | null | undefined): string | null {
  if (!onboarding || shouldShowFloristInviteResend(onboarding)) return null;
  return 'A conta da florista já está ativa (por exemplo, ao associar uma conta existente). Não há convite pendente para reenviar.';
}
