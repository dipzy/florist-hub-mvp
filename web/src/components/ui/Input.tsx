import type { InputHTMLAttributes } from 'react';

type Props = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
};

export function Input({ label, hint, error, id, className = '', ...rest }: Props) {
  const inputId = id ?? `input-${label.replace(/\s+/g, '-').toLowerCase()}`;
  return (
    <label className={`fh-field${error ? ' fh-field--error' : ''} ${className}`.trim()} htmlFor={inputId}>
      <span className="fh-field__label">{label}</span>
      <input id={inputId} className="fh-input" aria-invalid={error ? true : undefined} {...rest} />
      {hint && !error && <span className="fh-field__hint">{hint}</span>}
      {error && <span className="fh-field__error">{error}</span>}
    </label>
  );
}
