import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '../api/client';
import type { PublicOrderTracking } from '../api/types';
import { deliveryTypeLabel, orderStatusLabel } from '../lib/enumLabels';
import { formatDate } from '../lib/locale';
import { OrderStatusBadge } from '../components/ui/Badge';
import { ErrorState } from '../components/ui/ErrorState';
import { LoadingState } from '../components/ui/LoadingState';

function formatDeliveryWindow(
  tracking: PublicOrderTracking,
  t: (key: string) => string,
) {
  if (tracking.deliveryWindowLabel) {
    const range =
      tracking.deliveryWindowStart && tracking.deliveryWindowEnd
        ? `${tracking.deliveryWindowStart}–${tracking.deliveryWindowEnd}`
        : null;
    return range ? `${tracking.deliveryWindowLabel} (${range})` : tracking.deliveryWindowLabel;
  }
  if (tracking.requestedDeliveryAt) {
    return formatDate(tracking.requestedDeliveryAt, { dateStyle: 'short', timeStyle: 'short' });
  }
  if (tracking.capacityServiceDate) {
    return formatDate(`${tracking.capacityServiceDate}T12:00:00`, { dateStyle: 'medium' });
  }
  return t('tracking.confirmWithFlorist');
}

export function OrderTrackingPage() {
  const { t } = useTranslation();
  const { token } = useParams<{ token: string }>();
  const [tracking, setTracking] = useState<PublicOrderTracking | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    api
      .trackOrder(token)
      .then(setTracking)
      .catch(() =>
        setError(t('tracking.loadError')),
      );
  }, [token, t]);

  if (error) return <ErrorState message={error} />;
  if (!tracking) return <LoadingState message={t('tracking.loading')} />;

  return (
    <section className="page">
      <h1>{t('tracking.title')}</h1>
      <p className="muted">
        {t('tracking.ref')} #{tracking.orderId.slice(0, 8)}
      </p>
      <article className="card stack">
        <div className="row spread" style={{ alignItems: 'flex-start' }}>
          <div>
            <p className="stat-label">{t('tracking.statusHeading')}</p>
            <OrderStatusBadge
              status={tracking.status}
              label={orderStatusLabel(tracking.status, t)}
            />
          </div>
          <div>
            <p className="stat-label">{t('tracking.deliveryHeading')}</p>
            <p>{deliveryTypeLabel(tracking.deliveryType, t)}</p>
          </div>
        </div>
        <div>
          <p className="stat-label">{t('tracking.windowHeading')}</p>
          <p>{formatDeliveryWindow(tracking, t)}</p>
        </div>
        {tracking.timeline.length > 0 && (
          <div>
            <p className="stat-label">{t('tracking.timelineHeading')}</p>
            <ul className="stack timeline">
              {tracking.timeline.map((step) => (
                <li key={`${step.status}-${step.at}`}>
                  <strong>{orderStatusLabel(step.status, t)}</strong>{' '}
                  <span className="muted">
                    {formatDate(step.at, { dateStyle: 'short', timeStyle: 'short' })}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </article>
      <p>
        <Link to="/portfolio">{t('tracking.backToShop')}</Link>
      </p>
    </section>
  );
}
