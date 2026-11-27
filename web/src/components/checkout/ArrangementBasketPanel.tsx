import { useState } from 'react';
import { Minus, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '../ui/Button';
import { Modal } from '../ui/Modal';
import type { BasketLine } from '../../lib/basket';
import {
  adjustLineQuantity,
  basketItemsTotal,
  basketLineKey,
  clearBasketLines,
  removeBasketLine,
} from '../../lib/basket';
import { formatMoney } from '../../lib/locale';

type Props = {
  basket: BasketLine[];
  deliveryFee?: number;
  estimatedTotal: number;
  onChange: (next: BasketLine[]) => void;
  disabled?: boolean;
  showContinue?: boolean;
  onContinue?: () => void;
};

export function ArrangementBasketPanel({
  basket,
  deliveryFee = 0,
  estimatedTotal,
  onChange,
  disabled,
  showContinue,
  onContinue,
}: Props) {
  const { t } = useTranslation();
  const [confirmClear, setConfirmClear] = useState(false);
  const itemsTotal = basketItemsTotal(basket);
  const basketEmpty = basket.length === 0;

  function clearAll() {
    if (basketEmpty) return;
    setConfirmClear(true);
  }

  return (
    <>
    <section className="card arrangement-basket-panel" aria-labelledby="arrangement-basket-heading">
      <header className="arrangement-basket-panel__header">
        <h2 id="arrangement-basket-heading">{t('build.yourArrangement')}</h2>
        <button
          type="button"
          className="fh-btn fh-btn--ghost fh-btn--sm"
          disabled={disabled || basketEmpty}
          onClick={clearAll}
        >
          {t('build.clearAll')}
        </button>
      </header>

      {basketEmpty ? (
        <p className="muted">{t('build.emptyBasket')}</p>
      ) : (
        <ul className="arrangement-basket-panel__lines">
          {basket.map((line) => {
            const key = basketLineKey(line);
            const lineTotal = line.quantity * line.unitPrice;
            return (
              <li key={key} className="arrangement-basket-panel__line">
                <div className="arrangement-basket-panel__line-info">
                  <span className="arrangement-basket-panel__line-name">{line.name}</span>
                  <span className="muted arrangement-basket-panel__line-total">{formatMoney(lineTotal)}</span>
                </div>
                <div className="arrangement-basket-panel__line-actions">
                  <div className="arrangement-basket-panel__qty">
                    <button
                      type="button"
                      className="fh-btn fh-btn--ghost fh-btn--icon"
                      disabled={disabled}
                      onClick={() => onChange(adjustLineQuantity(basket, key, -1))}
                      aria-label={t('build.decreaseQty')}
                    >
                      <Minus size={16} />
                    </button>
                    <span className="arrangement-basket-panel__count" aria-live="polite">
                      {line.quantity}
                    </span>
                    <button
                      type="button"
                      className="fh-btn fh-btn--ghost fh-btn--icon"
                      disabled={disabled}
                      onClick={() => onChange(adjustLineQuantity(basket, key, 1))}
                      aria-label={t('build.increaseQty')}
                    >
                      <Plus size={16} />
                    </button>
                  </div>
                  <button
                    type="button"
                    className="fh-btn fh-btn--ghost fh-btn--icon"
                    disabled={disabled}
                    onClick={() => onChange(removeBasketLine(basket, key))}
                    aria-label={t('build.removeLine', { name: line.name })}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <dl className="arrangement-basket-panel__totals">
        <div>
          <dt>{t('build.subtotal')}</dt>
          <dd>{formatMoney(itemsTotal)}</dd>
        </div>
        {deliveryFee > 0 && (
          <div>
            <dt>{t('build.deliveryFee')}</dt>
            <dd>{formatMoney(deliveryFee)}</dd>
          </div>
        )}
        <div className="arrangement-basket-panel__total">
          <dt>{t('build.estimatedTotal')}</dt>
          <dd>{formatMoney(estimatedTotal)}</dd>
        </div>
      </dl>

      {showContinue && (
        <div className="arrangement-basket-panel__continue">
          <button type="button" disabled={basketEmpty || disabled} onClick={onContinue} title={basketEmpty ? t('build.addFlowersHint') : undefined}>
            {t('build.continueDelivery')}
          </button>
          {basketEmpty && (
            <p className="muted" role="status">
              {t('build.addFlowersHint')}
            </p>
          )}
        </div>
      )}
    </section>
    <Modal
      open={confirmClear}
      title={t('build.clearAll')}
      onClose={() => setConfirmClear(false)}
      footer={
        <>
          <Button type="button" variant="ghost" onClick={() => setConfirmClear(false)}>
            {t('common.cancel')}
          </Button>
          <Button
            type="button"
            variant="danger"
            onClick={() => {
              onChange(clearBasketLines());
              setConfirmClear(false);
            }}
          >
            {t('build.clearAll')}
          </Button>
        </>
      }
    >
      <p>{t('build.clearAllConfirm')}</p>
    </Modal>
    </>
  );
}
