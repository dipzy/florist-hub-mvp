import type { ChangeEvent, ReactNode, SelectHTMLAttributes } from 'react';
import { ListboxSelect, optionsFromNodes } from './ListboxSelect';

type Props = Omit<SelectHTMLAttributes<HTMLSelectElement>, 'children'> & {
  children?: ReactNode;
  label?: string;
};

/** Substitui um `<select>` nativo por um listbox com popover. O `onChange` recebe `target.value`. */
export function InlineSelect({ children, value, onChange, disabled, id, label, 'aria-label': ariaLabel }: Props) {
  const options = optionsFromNodes(children);
  const current = value == null ? '' : String(value);
  const name = label || ariaLabel || options.find((option) => option.value === current)?.label || 'Opção';

  return (
    <ListboxSelect
      id={id}
      label={name}
      hideLabel={!label}
      value={current}
      options={options}
      disabled={disabled}
      onChange={(next) => {
        onChange?.({ target: { value: next }, currentTarget: { value: next } } as ChangeEvent<HTMLSelectElement>);
      }}
    />
  );
}
