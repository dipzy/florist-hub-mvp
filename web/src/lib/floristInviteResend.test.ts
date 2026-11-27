import { describe, expect, it } from 'vitest';
import {
  floristInviteResendUnavailableMessage,
  shouldShowFloristInviteResend,
} from './floristInviteResend';
import type { PlatformTenantOnboarding } from '../api/types';

const base: PlatformTenantOnboarding = {
  tenantId: 't1',
  name: 'Loja',
  slug: 'loja',
  isPublished: false,
  floristInviteCompleted: false,
  checklist: [],
  completedRequiredCount: 0,
  requiredCount: 5,
};

describe('floristInviteResend', () => {
  it('mostra reenviar apenas com convite pendente', () => {
    expect(shouldShowFloristInviteResend(null)).toBe(false);
    expect(shouldShowFloristInviteResend({ ...base, floristInviteCompleted: false })).toBe(true);
    expect(shouldShowFloristInviteResend({ ...base, floristInviteCompleted: true })).toBe(false);
  });

  it('explica quando o reenvio não se aplica', () => {
    expect(floristInviteResendUnavailableMessage({ ...base, floristInviteCompleted: true })).toMatch(
      /conta da florista já está ativa/,
    );
    expect(floristInviteResendUnavailableMessage({ ...base, floristInviteCompleted: false })).toBeNull();
  });
});
