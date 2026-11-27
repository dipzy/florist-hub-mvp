/** @vitest-environment jsdom */
import { render, waitFor } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import { describe, expect, it, vi } from 'vitest';
import i18n, { i18nReady } from '../i18n';
import { Layout } from '../components/Layout';
import { AuthProvider } from '../context/AuthContext';
import { UiPreferencesProvider } from '../context/UiPreferencesContext';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { assertNoRenderedPlaceholders } from './i18nPlaceholders';

vi.mock('../api/client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api/client')>();
  return {
    ...actual,
    api: {
      ...actual.api,
      storeCheckout: vi.fn(),
    },
  };
});

vi.mock('../lib/publicTenant', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../lib/publicTenant')>();
  return {
    ...actual,
    resolveExplicitPublicTenantSlug: vi.fn(() => undefined),
    ensurePublicTenantResolved: vi.fn(async () => undefined),
  };
});

const RAW_KEY = /^[a-z][a-zA-Z0-9]*(\.[a-zA-Z0-9_-]+)+$/;

function assertNoRawKeys(text: string) {
  assertNoRenderedPlaceholders(text, 'texto renderizado');
  const tokens = text.split(/\s+/).filter(Boolean);
  for (const token of tokens) {
    if (RAW_KEY.test(token)) {
      throw new Error(`Chave de tradução crua visível: ${token}`);
    }
  }
}

function renderLayout(locale: 'pt-PT' | 'en') {
  return i18nReady.then(() => i18n.changeLanguage(locale)).then(() =>
    render(
      <I18nextProvider i18n={i18n}>
        <UiPreferencesProvider>
          <AuthProvider>
            <MemoryRouter>
              <Layout />
            </MemoryRouter>
          </AuthProvider>
        </UiPreferencesProvider>
      </I18nextProvider>,
    ),
  );
}

describe('i18n render', () => {
  it('pt-PT: cabeçalho da loja sem chaves cruas', async () => {
    await renderLayout('pt-PT');
    const nav = document.querySelector('header nav');
    expect(nav).toBeTruthy();
    assertNoRawKeys(nav!.textContent ?? '');
    expect(nav!.textContent).toMatch(/Loja|Portfolio/i);
  });

  it('en: cabeçalho da loja sem chaves cruas', async () => {
    await renderLayout('en');
    const nav = document.querySelector('header nav');
    expect(nav).toBeTruthy();
    assertNoRawKeys(nav!.textContent ?? '');
  });

  it('login da plataforma mostra «Entrar» sem {{storeName}}', async () => {
    await i18nReady;
    await i18n.changeLanguage('pt-PT');
    render(
      <I18nextProvider i18n={i18n}>
        <UiPreferencesProvider>
          <AuthProvider>
            <MemoryRouter initialEntries={['/login']}>
              <LoginPage />
            </MemoryRouter>
          </AuthProvider>
        </UiPreferencesProvider>
      </I18nextProvider>,
    );
    await waitFor(() => {
      const heading = document.querySelector('.auth-page h1');
      expect(heading?.textContent).toBe('Entrar');
    });
    assertNoRenderedPlaceholders(document.body.textContent ?? '', 'login plataforma');
  });
});
