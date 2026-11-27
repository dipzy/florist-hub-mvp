import { describe, expect, it } from 'vitest';
import { isActiveSessionContext, sessionContextLabel, shouldShowSessionSwitcher } from './sessionContextLabel';

const t = (key: string) =>
  (
    {
      'session.superAdminPlatform': 'SuperAdmin (plataforma)',
      'session.storeFallback': 'Loja',
    } as Record<string, string>
  )[key] ?? key;

describe('sessionContextLabel', () => {
  it('formata SuperAdmin e lojas', () => {
    expect(sessionContextLabel({ kind: 'SuperAdmin' }, t)).toBe('SuperAdmin (plataforma)');
    expect(sessionContextLabel({ kind: 'Florist', tenantName: 'Flores da Lina' }, t)).toBe('Flores da Lina');
  });

  it('só mostra o seletor com mais de um contexto', () => {
    expect(shouldShowSessionSwitcher(undefined)).toBe(false);
    expect(shouldShowSessionSwitcher([{ kind: 'SuperAdmin' }])).toBe(false);
    expect(
      shouldShowSessionSwitcher([
        { kind: 'SuperAdmin' },
        { kind: 'Florist', tenantId: 'a', tenantName: 'Loja' },
      ]),
    ).toBe(true);
  });

  it('deteta o contexto ativo', () => {
    expect(isActiveSessionContext({ kind: 'SuperAdmin' }, 'SuperAdmin', undefined)).toBe(true);
    expect(isActiveSessionContext({ kind: 'Florist', tenantId: '111' }, 'Florista', '111')).toBe(true);
    expect(isActiveSessionContext({ kind: 'Florist', tenantId: '222' }, 'Florista', '111')).toBe(false);
  });
});
