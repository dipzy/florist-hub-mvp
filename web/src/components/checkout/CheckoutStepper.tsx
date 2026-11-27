import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';

type StepId = 1 | 2 | 3;

type Props = {
  active: StepId;
  onSelect: (step: StepId) => void;
  canGoDelivery: boolean;
  canGoConfirm: boolean;
};

export function CheckoutStepper({ active, onSelect, canGoDelivery, canGoConfirm }: Props) {
  const { t } = useTranslation();
  const steps: { id: StepId; label: string; enabled: boolean }[] = [
    { id: 1, label: t('build.steps.arrangement'), enabled: true },
    { id: 2, label: t('build.steps.delivery'), enabled: canGoDelivery },
    { id: 3, label: t('build.steps.confirm'), enabled: canGoConfirm },
  ];

  return (
    <nav className="checkout-stepper" aria-label={t('build.steps.aria')}>
      <ol>
        {steps.map((step, index) => {
          const done = step.id < active;
          const isActive = step.id === active;
          return (
            <li key={step.id} className={isActive ? 'is-active' : done ? 'is-done' : undefined}>
              <button
                type="button"
                className="fh-btn checkout-stepper__btn"
                disabled={!step.enabled && step.id > active}
                onClick={() => step.enabled && onSelect(step.id)}
                aria-current={isActive ? 'step' : undefined}
              >
                <span className="checkout-stepper__badge" aria-hidden>
                  {done ? <Check size={14} /> : index + 1}
                </span>
                <span className="checkout-stepper__label">{step.label}</span>
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
