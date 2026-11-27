import type { ReactNode } from 'react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ErrorState } from '../ui/ErrorState';
import { LoadingState } from '../ui/LoadingState';
import { Skeleton } from '../ui/Skeleton';

export type AdminLoadPhase = 'loading' | 'ready' | 'error';

export function useAdminLoad(loader: () => Promise<void>, deps: readonly unknown[]) {
  const [phase, setPhase] = useState<AdminLoadPhase>('loading');
  const [nonce, setNonce] = useState(0);
  const retry = useCallback(() => setNonce((current) => current + 1), []);

  useEffect(() => {
    let cancel = false;
    setPhase('loading');
    loader()
      .then(() => {
        if (!cancel) setPhase('ready');
      })
      .catch(() => {
        if (!cancel) setPhase('error');
      });
    return () => {
      cancel = true;
    };
    // O loader é o da renderização em que as dependências mudaram.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [nonce, ...deps]);

  return { phase, retry };
}

export function AdminLoadGate({
  phase,
  onRetry,
  children,
}: {
  phase: AdminLoadPhase;
  onRetry: () => void;
  children: ReactNode;
}) {
  const { t } = useTranslation();
  if (phase === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <LoadingState
          skeleton={
            <>
              <Skeleton variant="block" className="settings-skeleton" />
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line" />
            </>
          }
        />
      </div>
    );
  }
  if (phase === 'error') {
    return (
      <div className="page">
        <ErrorState message={t('admin.loadError')} onRetry={onRetry} />
      </div>
    );
  }
  return <>{children}</>;
}
