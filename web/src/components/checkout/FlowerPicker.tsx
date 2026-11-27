import { useTranslation } from 'react-i18next';
import { QuantityStepper } from '../ui/QuantityStepper';
import { mediaUrl } from '../../api/client';
import type { Flower } from '../../api/types';
import { isFlowerExhausted } from '../../lib/capacity';
import type { BasketLine } from '../../lib/basket';
import { quantityForFlowerId, setFlowerQuantity } from '../../lib/basket';
import { formatMoney } from '../../lib/locale';

type Props = {
  flowers: Flower[];
  basket: BasketLine[];
  disabled?: boolean;
  onChange: (next: BasketLine[]) => void;
};

export function FlowerPicker({ flowers, basket, disabled, onChange }: Props) {
  const { t } = useTranslation();

  function setQty(flower: Flower, quantity: number) {
    const exhausted = isFlowerExhausted(flower);
    if (exhausted && quantity > quantityForFlowerId(basket, flower.id)) return;
    onChange(setFlowerQuantity(basket, flower, quantity));
  }

  return (
    <div className="flower-picker-scroll" role="list">
      {flowers.length === 0 && (
        <p className="muted">{t('build.noCatalog')}</p>
      )}
      {flowers.map((f) => {
        const exhausted = isFlowerExhausted(f);
        const qty = quantityForFlowerId(basket, f.id);
        return (
          <article key={f.id} className="flower-picker-card" role="listitem">
            <div className="flower-picker-card__media">
              {f.imageUrl ? (
                <img src={mediaUrl(f.imageUrl)} alt="" width={56} height={56} loading="lazy" />
              ) : (
                <span className="flower-picker-card__placeholder" aria-hidden />
              )}
            </div>
            <div className="flower-picker-card__body">
              <p className="flower-picker-card__name">{f.name}</p>
              <p className="flower-picker-card__price muted">
                {formatMoney(f.unitPrice)}
                {exhausted ? ` · ${t('common.outOfStock')}` : ''}
              </p>
            </div>
            <QuantityStepper
              value={qty}
              onChange={(next) => setQty(f, next)}
              decreaseLabel={t('build.decreaseQty')}
              increaseLabel={t('build.increaseQty')}
              disabled={disabled || (exhausted && qty === 0)}
              max={exhausted ? qty : undefined}
            />
          </article>
        );
      })}
    </div>
  );
}
