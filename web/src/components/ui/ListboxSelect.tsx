import { useEffect, useId, useRef, useState, type ReactNode } from 'react';
import { Check, ChevronDown } from 'lucide-react';

export type ListboxOption = {
  value: string;
  label: string;
  disabled?: boolean;
};

type Props = {
  id?: string;
  label: string;
  hideLabel?: boolean;
  value: string;
  options: ListboxOption[];
  onChange: (value: string) => void;
  disabled?: boolean;
  placeholder?: string;
  className?: string;
};

export function ListboxSelect({
  id,
  label,
  hideLabel,
  value,
  options,
  onChange,
  disabled,
  placeholder,
  className,
}: Props) {
  const autoId = useId();
  const buttonId = id ?? autoId;
  const listId = `${buttonId}-list`;
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);
  const selected = options.find((option) => option.value === value);

  useEffect(() => {
    if (!open) return;
    function onPointer(ev: MouseEvent) {
      if (!wrapRef.current?.contains(ev.target as Node)) setOpen(false);
    }
    function onKey(ev: KeyboardEvent) {
      if (ev.key === 'Escape') setOpen(false);
    }
    document.addEventListener('mousedown', onPointer);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onPointer);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  function pick(next: string) {
    onChange(next);
    setOpen(false);
  }

  function onTriggerKey(ev: React.KeyboardEvent) {
    if (disabled) return;
    if (ev.key === 'ArrowDown' || ev.key === 'Enter' || ev.key === ' ') {
      ev.preventDefault();
      setOpen(true);
    }
  }

  return (
    <div className={`fh-listbox${className ? ` ${className}` : ''}`} ref={wrapRef}>
      <span className={hideLabel ? 'fh-sr-only' : 'fh-field__label'} id={`${buttonId}-label`}>
        {label}
      </span>
      <button
        type="button"
        id={buttonId}
        className="fh-btn fh-listbox__trigger"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-labelledby={`${buttonId}-label`}
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        onKeyDown={onTriggerKey}
      >
        <span>{selected?.label ?? placeholder ?? label}</span>
        <ChevronDown size={16} aria-hidden />
      </button>
      {open && (
        <ul id={listId} className="fh-listbox__panel" role="listbox" aria-labelledby={`${buttonId}-label`}>
          {options.map((option) => (
            <li key={option.value} role="presentation">
              <button
                type="button"
                className="fh-btn fh-listbox__option"
                role="option"
                aria-selected={option.value === value}
                disabled={option.disabled}
                onClick={() => pick(option.value)}
              >
                <span>{option.label}</span>
                {option.value === value ? <Check size={16} aria-hidden /> : <span className="fh-listbox__check-spacer" />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function optionsFromNodes(children: ReactNode): ListboxOption[] {
  const options: ListboxOption[] = [];
  const walk = (node: ReactNode) => {
    if (node == null || typeof node === 'boolean') return;
    if (Array.isArray(node)) {
      node.forEach(walk);
      return;
    }
    if (typeof node === 'object' && 'props' in node) {
      const el = node as { type?: unknown; props?: { value?: unknown; disabled?: boolean; children?: ReactNode } };
      if (el.type === 'option') {
        const raw = el.props?.value;
        options.push({
          value: raw == null ? '' : String(raw),
          label: textOf(el.props?.children),
          disabled: el.props?.disabled,
        });
        return;
      }
      walk(el.props?.children);
    }
  };
  walk(children);
  return options;
}

function textOf(node: ReactNode): string {
  if (node == null || typeof node === 'boolean') return '';
  if (typeof node === 'string' || typeof node === 'number') return String(node);
  if (Array.isArray(node)) return node.map(textOf).join('');
  if (typeof node === 'object' && 'props' in node) {
    return textOf((node as { props?: { children?: ReactNode } }).props?.children);
  }
  return '';
}
