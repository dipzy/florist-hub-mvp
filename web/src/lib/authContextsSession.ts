import type { AuthContextOption } from '../api/types';
import type { Session } from '../api/client';

/** Caminho da página de perfil consoante o papel na sessão atual. */
export function accountProfilePath(role: string | undefined): string | null {
  if (role === 'SuperAdmin') return '/superadmin/perfil';
  if (role === 'Florista') return '/admin/perfil';
  return null;
}

/** Destino após mudança de contexto (alinha com o router existente). */
export function homePathAfterContextSwitch(role: string): string {
  const r = role.trim();
  if (r === 'SuperAdmin') return '/superadmin';
  if (r === 'Florista' || r === 'Florist') return '/admin';
  return '/portfolio';
}

export function mergeContextsIntoSession(session: Session, contexts: AuthContextOption[]): Session {
  return { ...session, availableContexts: contexts };
}
