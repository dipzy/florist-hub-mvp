type Props = {
  className?: string;
  variant?: 'text' | 'block' | 'circle';
};

export function Skeleton({ className = '', variant = 'text' }: Props) {
  return <span className={`fh-skeleton fh-skeleton--${variant} ${className}`.trim()} aria-hidden />;
}

export function SkeletonGrid({ count = 3 }: { count?: number }) {
  return (
    <div className="grid" aria-busy="true" aria-label="A carregar">
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="fh-card fh-card--pad-md">
          <Skeleton variant="block" className="skeleton-hero" />
          <Skeleton className="skeleton-title" />
          <Skeleton className="skeleton-line" />
        </div>
      ))}
    </div>
  );
}
