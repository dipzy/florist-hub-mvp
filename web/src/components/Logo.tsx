import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { mediaUrl } from '../api/client';
import { useStoreBranding } from '../context/StoreBrandingContext';
import { PlatformLogoMark } from './PlatformLogoMark';

type Props = {
  showWordmark?: boolean;
  compact?: boolean;
};

function resolveTenantLogoSrc(logoUrl?: string | null): string | null {
  if (logoUrl == null) return null;
  const trimmed = logoUrl.trim();
  if (!trimmed) return null;
  return mediaUrl(trimmed);
}

export function Logo({ showWordmark = true, compact }: Props) {
  const { t } = useTranslation();
  const store = useStoreBranding();
  const size = compact ? 32 : 40;
  const tenantSrc = resolveTenantLogoSrc(store.logoUrl);
  const [tenantFailed, setTenantFailed] = useState(false);

  useEffect(() => {
    setTenantFailed(false);
  }, [store.logoUrl]);

  const showTenant = Boolean(tenantSrc) && !tenantFailed;

  return (
    <span className={`fh-logo${compact ? ' fh-logo--compact' : ''}`}>
      {showTenant ? (
        <img
          src={tenantSrc!}
          alt=""
          className="fh-logo__tenant"
          width={size}
          height={size}
          onError={() => setTenantFailed(true)}
        />
      ) : (
        <PlatformLogoMark size={size} className="fh-logo__mark" />
      )}
      {showWordmark && (
        <span className="fh-logo__text">
          <span className="fh-logo__title">{store.name ?? t('common.floristHub')}</span>
        </span>
      )}
    </span>
  );
}
