import { useId, useRef } from 'react';
import { normalizeHexColor } from '../../lib/normalizeHexColor';

type Props = {
  label: string;
  value: string;
  onChange: (next: string) => void;
  invalid?: boolean;
  fallback?: string;
};

export function ColorField({ label, value, onChange, invalid, fallback = '#8e3b5c' }: Props) {
  const pickerId = useId();
  const pickerRef = useRef<HTMLInputElement>(null);
  const pickerValue = normalizeHexColor(value) ?? fallback;

  function commitText(raw: string) {
    const norm = normalizeHexColor(raw);
    if (norm) onChange(norm);
  }

  return (
    <label className="theme-color-field">
      <span className="theme-color-field__label">{label}</span>
      <div className="theme-color-field__row">
        <button
          type="button"
          className="fh-btn theme-color-field__swatch"
          style={{ backgroundColor: pickerValue }}
          aria-label={label}
          onClick={() => pickerRef.current?.click()}
        />
        <input
          ref={pickerRef}
          id={pickerId}
          type="color"
          className="theme-color-field__picker"
          value={pickerValue}
          onChange={(ev) => onChange(ev.target.value.toLowerCase())}
          tabIndex={-1}
        />
        <input
          type="text"
          className="theme-color-field__hex fh-input"
          value={value}
          aria-invalid={invalid || undefined}
          spellCheck={false}
          onChange={(ev) => onChange(ev.target.value)}
          onBlur={(ev) => commitText(ev.target.value)}
        />
      </div>
    </label>
  );
}
