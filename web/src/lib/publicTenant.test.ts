/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from 'vitest';
import { getTenantQueryParam, hydratePublicTenantFromStorage, publicTenantHeaders, storeSlugFromHostname, tenantSlugCandidates, withTenantSearch } from './publicTenant';

describe('publicTenant', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('withTenantSearch acrescenta tenant quando guardado', () => {
    sessionStorage.setItem('floristhub.publicTenantSlug', 'sao-flores');
    expect(withTenantSearch('/portfolio')).toBe('/portfolio?tenant=sao-flores');
  });

  it('getTenantQueryParam lê da URL', () => {
    window.history.replaceState({}, '', '/?tenant=sao-flores');
    expect(getTenantQueryParam()).toBe('sao-flores');
  });

  it('withTenantSearch usa o tenant da página mesmo sem sessão', () => {
    window.history.replaceState({}, '', '/montar?tenant=sao-flores');
    expect(withTenantSearch('/flores')).toBe('/flores?tenant=sao-flores');
  });

  it('não tira o tenant de pages.dev, onrender, workers nem localhost', () => {
    expect(storeSlugFromHostname('florist-hub-26m.pages.dev', 'pages.dev')).toBeUndefined();
    expect(storeSlugFromHostname('florist-hub-26m.pages.dev')).toBeUndefined();
    expect(storeSlugFromHostname('florist-hub-api.onrender.com')).toBeUndefined();
    expect(storeSlugFromHostname('localhost')).toBeUndefined();
    expect(storeSlugFromHostname('sao-flores.localhost')).toBeUndefined();
    expect(storeSlugFromHostname('app.workers.dev', 'workers.dev')).toBeUndefined();
    expect(storeSlugFromHostname('sao-flores.floristhub.pt', 'floristhub.pt')).toBe('sao-flores');
  });

  it('prefere o tenant gravado e nunca pede florist-hub-26m', () => {
    expect(
      tenantSlugCandidates({
        hostname: 'florist-hub-26m.pages.dev',
        baseDomain: 'pages.dev',
        stored: 'sao-flores',
      }),
    ).toEqual(['sao-flores']);
  });

  it('repõe o id gravado para os pedidos seguintes', () => {
    sessionStorage.setItem('floristhub.publicTenantId', '0f01714d-7b6a-4355-80f0-fdb2840c95f5');
    sessionStorage.setItem('floristhub.publicTenantSlug', 'sao-flores');
    hydratePublicTenantFromStorage();
    expect(publicTenantHeaders()).toMatchObject({
      'X-Tenant-Id': '0f01714d-7b6a-4355-80f0-fdb2840c95f5',
      'X-Tenant-Slug': 'sao-flores',
    });
  });
});
