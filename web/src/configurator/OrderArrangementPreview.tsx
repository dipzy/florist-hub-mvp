import { lazy, Suspense, useEffect, useState } from 'react';
import { usePageText } from '../lib/usePageText';
import { api, mediaUrl } from '../api/client';
import type { Flower, Order } from '../api/types';
import { arrangementBaseLabel, arrangementShapeLabel } from '../lib/enumLabels';
import { useTranslation } from 'react-i18next';
import type { ArrangementBaseId, ArrangementShapeId } from './layout';

const ArrangementConfigurator = lazy(() =>
  import('./ArrangementConfigurator').then((module) => ({ default: module.ArrangementConfigurator })),
);

export function OrderArrangementPreview({
  order,
  showCanvas = true,
  showPreviewImage = true,
}: {
  order: Order;
  showCanvas?: boolean;
  showPreviewImage?: boolean;
}) {
  const { t } = useTranslation();
  const { pt } = usePageText('configurator/OrderArrangementPreview');
  const [flowers, setFlowers] = useState<Flower[]>([]);

  useEffect(() => {
    api.flowers().then(setFlowers).catch(() => setFlowers([]));
  }, []);

  const shape = (order.arrangementShape ?? 0) as ArrangementShapeId;
  const baseType = (order.baseType ?? 0) as ArrangementBaseId;
  const seed = order.arrangementVariationSeed ?? 0;
  const items = order.items.map((item) => {
    const flower =
      flowers.find((candidate) => candidate.id === item.flowerId) ??
      flowers.find((candidate) => candidate.name.toLowerCase() === item.name.toLowerCase());
    return { name: item.name, quantity: item.quantity, color: flower?.color };
  });

  return (
    <section className="order-arrangement">
      <h2>Arranjo 3D</h2>
      <p>
        <strong>{t('configurator.shapeHeading')}:</strong> {arrangementShapeLabel(shape, t)} ·{' '}
        <strong>{t('configurator.baseHeading')}:</strong> {arrangementBaseLabel(baseType, t)} ·{' '}
        <strong>{t('configurator.variation')}:</strong> {seed}
      </p>
      {showPreviewImage && order.arrangementPreviewUrl && (
        <img
          className="arrangement-preview"
          src={mediaUrl(order.arrangementPreviewUrl)}
          alt={pt("Pré-visualização do arranjo enviada com a encomenda")}
        />
      )}
      {showCanvas && (
        <Suspense fallback={<p className="muted">A preparar o ramo…</p>}>
          <ArrangementConfigurator items={items} shape={shape} baseType={baseType} seed={seed} readOnly />
        </Suspense>
      )}
    </section>
  );
}
