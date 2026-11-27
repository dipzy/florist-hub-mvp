/** @vitest-environment jsdom */
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { I18nextProvider } from 'react-i18next';
import i18n, { i18nReady } from '../i18n';
import { UiPreferencesProvider } from '../context/UiPreferencesContext';
import { AdminSidebarFooter } from './AdminSidebarFooter';

vi.mock('../context/AuthContext', () => ({
  useAuth: () => ({
    isAuthenticated: true,
    email: 'florista@exemplo.pt',
    role: 'Florista',
    logout: vi.fn(),
  }),
}));

describe('AdminSidebarFooter', () => {
  it('mostra inicial, email e ações Conta/Sair', async () => {
    await i18nReady;
    render(
      <I18nextProvider i18n={i18n}>
        <UiPreferencesProvider>
          <MemoryRouter>
            <AdminSidebarFooter />
          </MemoryRouter>
        </UiPreferencesProvider>
      </I18nextProvider>,
    );
    expect(screen.getByText('F')).toBeTruthy();
    expect(screen.getByTitle('florista@exemplo.pt')).toBeTruthy();
    expect(screen.getByRole('link', { name: /Conta/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Sair/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Aparência e idioma/i })).toBeTruthy();
  });
});
