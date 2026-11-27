import { useEffect, type ReactNode } from 'react';
import { api } from '../api/client';
import { applyStoreTheme } from '../lib/storeTheme';
import { ensurePublicTenantResolved } from '../lib/publicTenant';

export function PublicStoreShell({ children }: { children: ReactNode }) {
  useEffect(() => {
    let cancelled = false;
    (async () => {
      await ensurePublicTenantResolved();
      try {
        const theme = await api.storeTheme();
        if (!cancelled) applyStoreTheme(theme);
      } catch {
        if (!cancelled) applyStoreTheme(null);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return <div className="public-store">{children}</div>;
}
