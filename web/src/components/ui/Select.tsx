import type { ChangeEvent, ReactNode, SelectHTMLAttributes } from 'react';
import { ListboxSelect, optionsFromNodes } from './ListboxSelect';

type Props = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  children: ReactNode;
  hint?: string;
  error?: string;
};

export function Select({ label, hint, error, id, className = '', children, value, onChange, disabled }: Props) {
  const selectId = id ?? `select-${label.replace(/\s+/g, '-').toLowerCase()}`;
  const options = optionsFromNodes(children);
  const current = value == null ? '' : String(value);

  return (
    <div className={`fh-field${error ? ' fh-field--error' : ''} ${className}`.trim()}>
      <ListboxSelect
        id={selectId}
        label={label}
        value={current}
        options={options}
        disabled={disabled}
        onChange={(next) => {
          onChange?.({ target: { value: next }, currentTarget: { value: next } } as ChangeEvent<HTMLSelectElement>);
        }}
      />
      {hint && !error && <span className="fh-field__hint">{hint}</span>}
      {error && <span className="fh-field__error">{error}</span>}
    </div>
  );
}
