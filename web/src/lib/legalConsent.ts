import type { LegalVersions } from '../api/types';

export type LegalAcceptance = {
  acceptTermsAndPrivacy: boolean;
  termsVersion: string;
  privacyVersion: string;
};

export function buildLegalAcceptance(versions: LegalVersions, checked: boolean): LegalAcceptance {
  return {
    acceptTermsAndPrivacy: checked,
    termsVersion: versions.termsVersion,
    privacyVersion: versions.privacyVersion,
  };
}
