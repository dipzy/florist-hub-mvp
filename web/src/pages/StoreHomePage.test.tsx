/** @vitest-environment jsdom */
import { render, waitFor } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import i18n, { i18nReady } from '../i18n';
import { AuthProvider } from '../context/AuthContext';
import { StoreBrandingProvider } from '../context/StoreBrandingContext';
import { StoreHomePage } from './StorePublicPages';

const tenantGate = vi.hoisted(() => {
  let ready = false;
  let release: () => void = () => {};
  const promise = new Promise<void>((resolve) => {
    release = () => {
      ready = true;
      resolve();
    };
  });
  const description =
    'Na Florista São Flores fazemos ramos e arranjos à mão, todos os dias, com flores frescas escolhidas pela manhã. Do ramo para dizer obrigado ao arranjo para o casamento, tratamos de cada encomenda com carinho e entregamos em Leiria e arredores.';
  return {
    promise,
    release: () => release(),
    isReady: () => ready,
    description,
    home: {
      name: 'Florista São Flores',
      description,
      heroImageUrl: 'https://cdn.example/logo-loja.png',
      seasonalFlowers: [],
      averageRating: 0,
      reviewCount: 0,
    },
    arrangement: {
      id: 'p1',
      title: 'Centro de Mesa Primavera',
      description: 'Centro de mesa longo com gerberas.',
      status: 0,
      slug: 'centro-de-mesa-primavera',
      photos: [{ url: 'https://cdn.example/mesa.jpg', sortOrder: 0 }],
      items: [],
    },
    flower: {
      id: 'f1',
      name: 'Gerbera amarela',
      unitPrice: 3,
      imageUrl: 'https://cdn.example/gerbera.jpg',
      isApproximatelyAvailable: true,
    },
  };
});

vi.mock('../api/client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api/client')>();
  return {
    ...actual,
    api: {
      ...actual.api,
      storeCheckout: vi.fn(async () => {
        await tenantGate.promise;
        return { name: 'Florista São Flores', logoUrl: 'https://cdn.example/logo-loja.png', openingHours: [] };
      }),
      storeTheme: vi.fn(async () => null),
      storeHome: vi.fn(async () => {
        if (!tenantGate.isReady()) throw new Error('tenant ainda não resolvido');
        return tenantGate.home;
      }),
      portfolio: vi.fn(async () => {
        if (!tenantGate.isReady()) throw new Error('tenant ainda não resolvido');
        return [tenantGate.arrangement];
      }),
      flowers: vi.fn(async () => {
        if (!tenantGate.isReady()) throw new Error('tenant ainda não resolvido');
        return [tenantGate.flower];
      }),
    },
  };
});

vi.mock('../lib/publicTenant', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../lib/publicTenant')>();
  return {
    ...actual,
    ensurePublicTenantResolved: () => tenantGate.promise.then(() => '0f01714d-7b6a-4355-80f0-fdb2840c95f5'),
    resolveExplicitPublicTenantSlug: () => 'sao-flores',
  };
});

function renderHome() {
  return render(
    <I18nextProvider i18n={i18n}>
      <AuthProvider>
        <StoreBrandingProvider>
          <MemoryRouter>
            <StoreHomePage />
          </MemoryRouter>
        </StoreBrandingProvider>
      </AuthProvider>
    </I18nextProvider>,
  );
}

describe('StoreHomePage', () => {
  beforeEach(async () => {
    await i18nReady;
    await i18n.changeLanguage('pt-PT');
  });

  it('mostra nome, descrição e portfolio quando o tenant resolve mais tarde', async () => {
    const view = renderHome();
    expect(view.queryByRole('heading', { name: 'Centro de Mesa Primavera' })).toBeNull();
    expect(view.queryByRole('heading', { name: 'Gerbera amarela' })).toBeNull();
    expect(view.container.querySelectorAll('.card-hero--placeholder').length).toBeGreaterThan(0);

    tenantGate.release();

    await waitFor(() => {
      expect(view.getByRole('heading', { level: 1, name: 'Florista São Flores' })).toBeTruthy();
    });
    expect(view.getByText(/ramos e arranjos à mão/)).toBeTruthy();
    expect(view.getByRole('button', { name: 'Ler mais' })).toBeTruthy();
    expect(view.getByRole('heading', { name: 'Centro de Mesa Primavera' })).toBeTruthy();
    expect(view.getByRole('img', { name: 'Centro de Mesa Primavera' }).getAttribute('src')).toBe('https://cdn.example/mesa.jpg');
    expect(view.queryByRole('heading', { name: 'Gerbera amarela' })).toBeNull();
    expect(view.container.querySelector('.store-hero')?.getAttribute('style')).toContain('mesa.jpg');
  });
});
