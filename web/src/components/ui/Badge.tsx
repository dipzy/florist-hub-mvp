import type { ReactNode } from 'react';
import type { OrderStatusVariant } from '../../lib/orderStatus';

type Props = {
  variant?: OrderStatusVariant | 'neutral';
  children: ReactNode;
  className?: string;
};

export function Badge({ variant = 'neutral', children, className = '' }: Props) {
  return (
    <span className={`fh-badge fh-badge--${variant} ${className}`.trim()} role="status">
      {children}
    </span>
  );
}

export function OrderStatusBadge({ status, label }: { status: number; label: string }) {
  return <Badge variant={status <= 6 ? (['pending', 'confirmed', 'progress', 'ready', 'delivered', 'cancelled', 'rejected'][status] as OrderStatusVariant) : 'neutral'}>{label}</Badge>;
}
