import type { HTMLAttributes, ReactNode } from 'react';

export function Table({ children, className = '', ...rest }: HTMLAttributes<HTMLTableElement> & { children: ReactNode }) {
  return (
    <div className="fh-table-wrap">
      <table className={`fh-table ${className}`.trim()} {...rest}>
        {children}
      </table>
    </div>
  );
}
