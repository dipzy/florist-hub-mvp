import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { flushSync } from 'react-dom';
import { api, getSession, loadSessionFromStorage, saveSession, sessionFromAuth, type Session } from '../api/client';
import type { AuthContextOption } from '../api/types';
import { mergeContextsIntoSession } from '../lib/authContextsSession';
import { ensurePublicTenantResolved, getPublicTenantId, getPublicTenantSlug } from '../lib/publicTenant';

type AuthContextValue = {
  isAuthenticated: boolean;
  role?: string;
  email?: string;
  tenantId?: string;
  availableContexts?: Session['availableContexts'];
  login: (email: string, password: string) => Promise<string>;
  adoptSession: (session: Session) => void;
  switchContext: (kind: string, tenantId?: string) => Promise<string>;
  refreshContexts: () => Promise<AuthContextOption[]>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  loadSessionFromStorage();
  const [tick, setTick] = useState(0);
  const session = getSession();

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: Boolean(session?.token),
      role: session?.role,
      email: session?.email,
      tenantId: session?.tenantId,
      availableContexts: session?.availableContexts,
      login: async (email, password) => {
        await ensurePublicTenantResolved();
        const tenantId = getPublicTenantId();
        const tenantSlug = getPublicTenantSlug();
        const res =
          tenantId || tenantSlug
            ? await api.login({
                email,
                password,
                ...(tenantId ? { tenantId } : {}),
                ...(tenantSlug ? { tenantSlug } : {}),
              })
            : await api.loginWithoutTenant({ email, password });
        saveSession(sessionFromAuth(res));
        setTick((x) => x + 1);
        return res.role;
      },
      adoptSession: (next) => {
        saveSession(next);
        setTick((x) => x + 1);
      },
      switchContext: async (kind, tenantId) => {
        const res = await api.switchAuthContext({ kind, tenantId });
        flushSync(() => {
          saveSession(sessionFromAuth(res));
          setTick((x) => x + 1);
        });
        return res.role;
      },
      refreshContexts: async () => {
        const contexts = await api.authContexts();
        const current = getSession();
        if (current) {
          saveSession(mergeContextsIntoSession(current, contexts));
          setTick((x) => x + 1);
        }
        return contexts;
      },
      logout: () => {
        saveSession(null);
        setTick((x) => x + 1);
      },
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [tick, session?.token],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('AuthProvider em falta');
  return ctx;
}
