import { useEffect, type ReactNode } from 'react';
import { Button } from './Button';

type Props = {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
};

export function Modal({ open, title, onClose, children, footer }: Props) {
  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fh-modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="fh-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="fh-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="fh-modal__header">
          <h2 id="fh-modal-title">{title}</h2>
          <Button type="button" variant="ghost" size="sm" onClick={onClose} aria-label="Fechar">
            ×
          </Button>
        </header>
        <div className="fh-modal__body">{children}</div>
        {footer && <footer className="fh-modal__footer">{footer}</footer>}
      </div>
    </div>
  );
}
