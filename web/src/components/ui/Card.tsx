import type { HTMLAttributes, ReactNode } from 'react';

type Props = HTMLAttributes<HTMLDivElement> & {
  children: ReactNode;
  padding?: 'sm' | 'md' | 'lg';
  elevated?: boolean;
};

export function Card({ children, className = '', padding = 'md', elevated, ...rest }: Props) {
  return (
    <div
      className={`fh-card fh-card--pad-${padding}${elevated ? ' fh-card--elevated' : ''} ${className}`.trim()}
      {...rest}
    >
      {children}
    </div>
  );
}
