import type { ReactNode } from 'react';

type Props = {
  title: string;
  description?: string;
  action?: ReactNode;
  icon?: ReactNode;
};

export function EmptyState({ title, description, action, icon }: Props) {
  return (
    <div className="fh-empty" role="status">
      {icon && <div className="fh-empty__icon" aria-hidden>{icon}</div>}
      <h3>{title}</h3>
      {description && <p className="muted">{description}</p>}
      {action && <div className="fh-empty__action">{action}</div>}
    </div>
  );
}
