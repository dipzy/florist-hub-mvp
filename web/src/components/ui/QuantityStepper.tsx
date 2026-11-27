import { Minus, Plus } from 'lucide-react';

type Props = {
  value: number;
  onChange: (next: number) => void;
  decreaseLabel: string;
  increaseLabel: string;
  min?: number;
  max?: number;
  disabled?: boolean;
};

export function QuantityStepper({ value, onChange, decreaseLabel, increaseLabel, min = 0, max, disabled }: Props) {
  return (
    <div className="fh-qty">
      <button
        type="button"
        className="fh-btn fh-btn--ghost fh-btn--icon"
        aria-label={decreaseLabel}
        disabled={disabled || value <= min}
        onClick={() => onChange(value - 1)}
      >
        <Minus size={16} aria-hidden />
      </button>
      <span className="fh-qty__value">{value}</span>
      <button
        type="button"
        className="fh-btn fh-btn--ghost fh-btn--icon"
        aria-label={increaseLabel}
        disabled={disabled || (max != null && value >= max)}
        onClick={() => onChange(value + 1)}
      >
        <Plus size={16} aria-hidden />
      </button>
    </div>
  );
}
