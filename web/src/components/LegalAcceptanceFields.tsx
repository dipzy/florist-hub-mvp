import { useTranslation } from 'react-i18next';
import { TenantLink } from './TenantLink';
import type { LegalVersions } from '../api/types';

type Props = {
  versions: LegalVersions | null;
  checked: boolean;
  marketing?: boolean;
  onCheckedChange: (v: boolean) => void;
  onMarketingChange?: (v: boolean) => void;
  showMarketing?: boolean;
};

export function LegalAcceptanceFields({
  versions,
  checked,
  marketing,
  onCheckedChange,
  onMarketingChange,
  showMarketing,
}: Props) {
  const { t } = useTranslation();

  return (
    <fieldset className="legal-acceptance">
      <label className="checkbox-row legal-acceptance__row">
        <input type="checkbox" required checked={checked} onChange={(e) => onCheckedChange(e.target.checked)} />
        <span>
          {t('legal.accept.lede')}{' '}
          <TenantLink to="/legal/termos" target="_blank" rel="noreferrer">
            {t('footer.terms')}
          </TenantLink>{' '}
          {t('legal.accept.and')}{' '}
          <TenantLink to="/legal/privacidade" target="_blank" rel="noreferrer">
            {t('footer.privacy')}
          </TenantLink>
          {versions ? ` (${t('legal.accept.version', { version: versions.termsVersion })}).` : '.'}
        </span>
      </label>
      {showMarketing && onMarketingChange && (
        <label className="checkbox-row legal-acceptance__row">
          <input type="checkbox" checked={!!marketing} onChange={(e) => onMarketingChange(e.target.checked)} />
          <span>{t('legal.accept.marketing')}</span>
        </label>
      )}
    </fieldset>
  );
}
