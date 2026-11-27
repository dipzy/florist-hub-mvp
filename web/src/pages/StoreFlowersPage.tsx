import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, mediaUrl } from '../api/client';
import type { Flower } from '../api/types';
import { formatMoney } from '../lib/locale';
import { showUnavailableNow } from '../lib/publicFlowerAvailability';
import { TenantLink } from '../components/TenantLink';
import { LoadingState } from '../components/ui/LoadingState';
import { EmptyState } from '../components/ui/EmptyState';

export function StoreFlowersPage() {
  const { t } = useTranslation();
  const [flowers, setFlowers] = useState<Flower[] | null>(null);

  useEffect(() => {
    api
      .flowers()
      .then(setFlowers)
      .catch(() => setFlowers([]));
  }, []);

  if (flowers === null) return <LoadingState message={t('common.loading')} />;

  return (
    <section className="page">
      <header className="page-header">
        <h1>{t('nav.flowers', { defaultValue: 'Flores' })}</h1>
        <p className="lede muted">{t('store.flowersCatalogLede', { defaultValue: 'Catálogo da loja para inspirar o seu arranjo.' })}</p>
      </header>
      {flowers.length === 0 ? (
        <EmptyState
          title={t('store.noFlowersTitle', { defaultValue: 'Sem flores no catálogo' })}
          description={t('store.noSeasonalDescription')}
        />
      ) : (
        <ul className="grid store-flowers-grid">
          {flowers.map((f) => (
            <li key={f.id} className="card">
              {f.imageUrl ? (
                <img className="card-hero" src={mediaUrl(f.imageUrl)} alt={f.name} loading="lazy" />
              ) : (
                <div className="card-hero card-hero--placeholder" aria-hidden />
              )}
              <h3>{f.name}</h3>
              <p className="muted">
                {formatMoney(f.unitPrice)}
                {showUnavailableNow(f) ? ` · ${t('common.unavailableNow')}` : ''}
              </p>
            </li>
          ))}
        </ul>
      )}
      <p className="store-flowers-cta">
        <TenantLink to="/montar" className="fh-btn fh-btn--primary fh-btn--md">
          {t('nav.buildArrangement')}
        </TenantLink>
      </p>
    </section>
  );
}
