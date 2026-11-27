import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api } from '../api/client';
import { ensurePublicTenantResolved, resolveExplicitPublicTenantSlug } from '../lib/publicTenant';
import { applyStoreTheme, readCachedStoreTheme } from '../lib/storeTheme';
import { useAuth } from './AuthContext';

type StoreBranding = {
  name?: string;
  logoUrl?: string;
  loading: boolean;
};

const StoreBrandingContext = createContext<StoreBranding>({ loading: true });

function seedBranding(): StoreBranding {
  const cached = readCachedStoreTheme(resolveExplicitPublicTenantSlug());
  return cached?.name ? { name: cached.name, loading: true } : { loading: true };
}

export function StoreBrandingProvider({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const [branding, setBranding] = useState<StoreBranding>(seedBranding);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      await ensurePublicTenantResolved();
      try {
        const [checkout, theme] = await Promise.all([api.storeCheckout(), api.storeTheme().catch(() => null)]);
        if (cancelled) return;
        if (theme) applyStoreTheme(theme);
        setBranding({
          name: checkout.name || theme?.name,
          logoUrl: checkout.logoUrl,
          loading: false,
        });
      } catch {
        if (!cancelled) setBranding((current) => ({ ...current, loading: false }));
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [auth.tenantId, auth.isAuthenticated]);

  const value = useMemo(() => branding, [branding]);

  return <StoreBrandingContext.Provider value={value}>{children}</StoreBrandingContext.Provider>;
}

export function useStoreBranding() {
  return useContext(StoreBrandingContext);
}
