import { describe, expect, it } from 'vitest';
import type { AuthContextOption } from '../api/types';
import type { Session } from '../api/client';
import {
  accountProfilePath,
  homePathAfterContextSwitch,
  mergeContextsIntoSession,
} from './authContextsSession';

describe('authContextsSession', () => {
  it('define o caminho da página de perfil (Conta)', () => {
    expect(accountProfilePath('SuperAdmin')).toBe('/superadmin/perfil');
    expect(accountProfilePath('Florista')).toBe('/admin/perfil');
    expect(accountProfilePath('Cliente')).toBeNull();
    expect(accountProfilePath(undefined)).toBeNull();
  });

  it('define o destino após mudança de contexto', () => {
    expect(homePathAfterContextSwitch('SuperAdmin')).toBe('/superadmin');
    expect(homePathAfterContextSwitch('Florista')).toBe('/admin');
    expect(homePathAfterContextSwitch('Cliente')).toBe('/portfolio');
  });

  it('atualiza availableContexts na sessão sem alterar o token', () => {
    const session: Session = {
      token: 'jwt-example',
      role: 'SuperAdmin',
      email: 'admin@example.test',
      availableContexts: [{ kind: 'SuperAdmin' }],
    };
    const fresh: AuthContextOption[] = [
      { kind: 'SuperAdmin' },
      { kind: 'Florist', tenantId: 't-new', tenantName: 'Florista São Flores' },
    ];
    const merged = mergeContextsIntoSession(session, fresh);
    expect(merged.token).toBe(session.token);
    expect(merged.availableContexts).toEqual(fresh);
    expect(merged.availableContexts).toHaveLength(2);
    expect(merged.availableContexts?.[1]?.tenantName).toBe('Florista São Flores');
  });
});
