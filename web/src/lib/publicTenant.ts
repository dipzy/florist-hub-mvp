import { api } from '../api/client';

const TENANT_SLUG_STORAGE_KEY = 'floristhub.publicTenantSlug';
const TENANT_ID_STORAGE_KEY = 'floristhub.publicTenantId';

const PLATFORM_HOST_SUFFIXES = ['.pages.dev', '.workers.dev', '.onrender.com', '.localhost', '.local'];

let publicTenantId: string | undefined;
let publicTenantSlug: string | undefined;
let publicTenantName: string | undefined;

export function readStoredPublicTenantSlug(): string | undefined {
  return readStoredSlug();
}

function readStoredSlug(): string | undefined {
  try {
    const raw = sessionStorage.getItem(TENANT_SLUG_STORAGE_KEY);
    return raw?.trim().toLowerCase() || undefined;
  } catch {
    return undefined;
  }
}

function readStoredId(): string | undefined {
  try {
    const raw = sessionStorage.getItem(TENANT_ID_STORAGE_KEY);
    return raw?.trim() || undefined;
  } catch {
    return undefined;
  }
}

function persistTenant(id: string, slug: string) {
  publicTenantId = id;
  publicTenantSlug = slug;
  try {
    sessionStorage.setItem(TENANT_SLUG_STORAGE_KEY, slug);
    sessionStorage.setItem(TENANT_ID_STORAGE_KEY, id);
  } catch {
    /* private mode */
  }
}

/** Repõe o tenant já gravado antes de qualquer pedido, mesmo que a resolução de rede falhe. */
export function hydratePublicTenantFromStorage() {
  if (!publicTenantId) publicTenantId = readStoredId();
  if (!publicTenantSlug) publicTenantSlug = readStoredSlug();
}

export function getPublicTenantId() {
  return publicTenantId;
}

export function getPublicTenantSlug() {
  return publicTenantSlug;
}

export function getPublicTenantName() {
  return publicTenantName;
}

export function getTenantQueryParam() {
  if (typeof window === 'undefined') return undefined;
  const raw = new URLSearchParams(window.location.search).get('tenant');
  return raw?.trim().toLowerCase() || undefined;
}

function isPlatformHostname(host: string): boolean {
  const normalized = host.trim().toLowerCase().replace(/\.$/, '');
  if (!normalized || normalized === 'localhost' || normalized === '127.0.0.1' || normalized === '::1' || normalized === '[::1]') {
    return true;
  }
  return PLATFORM_HOST_SUFFIXES.some((suffix) => normalized === suffix.slice(1) || normalized.endsWith(suffix));
}

/** Slug só a partir de um subdomínio do domínio de loja configurado. Pages, Render e localhost não contam. */
export function storeSlugFromHostname(hostname: string, baseDomain?: string | null): string | undefined {
  const host = hostname.trim().toLowerCase().replace(/\.$/, '');
  const base = baseDomain?.trim().toLowerCase().replace(/\.$/, '') || '';
  if (!host || !base || isPlatformHostname(host) || isPlatformHostname(base)) return undefined;
  if (host === base || host === `www.${base}`) return undefined;
  const suffix = `.${base}`;
  if (!host.endsWith(suffix)) return undefined;
  const sub = host.slice(0, -suffix.length);
  if (!sub || sub.includes('.') || sub === 'www') return undefined;
  return sub;
}

/** Ordem: query, domínio de loja, valor gravado, omissão. Nunca inclui o host da plataforma. */
export function tenantSlugCandidates(input: {
  hostname?: string;
  baseDomain?: string | null;
  query?: string | null;
  stored?: string | null;
  fallback?: string | null;
}): string[] {
  const hostSlug = input.hostname ? storeSlugFromHostname(input.hostname, input.baseDomain) : undefined;
  const values = [input.query, hostSlug, input.stored, input.fallback];
  const slugs = values.map((value) => value?.trim().toLowerCase()).filter((value): value is string => Boolean(value));
  return [...new Set(slugs)];
}

function resolveSlugFromHost(): string | undefined {
  if (typeof window === 'undefined') return undefined;
  return storeSlugFromHostname(window.location.hostname, import.meta.env.VITE_BASE_DOMAIN as string | undefined);
}

/** Mantém ?tenant= nos links quando a loja foi resolvida por query (ex. Pages). */
export function withTenantSearch(path: string): string {
  if (typeof window === 'undefined') return path;
  const slug = publicTenantSlug ?? getTenantQueryParam() ?? readStoredSlug();
  if (!slug || resolveSlugFromHost()) return path;

  const hashIdx = path.indexOf('#');
  const base = hashIdx >= 0 ? path.slice(0, hashIdx) : path;
  const hash = hashIdx >= 0 ? path.slice(hashIdx) : '';
  const qIdx = base.indexOf('?');
  const pathname = qIdx >= 0 ? base.slice(0, qIdx) : base;
  const params = new URLSearchParams(qIdx >= 0 ? base.slice(qIdx + 1) : '');
  if (!params.has('tenant')) params.set('tenant', slug);
  const query = params.toString();
  return `${pathname}${query ? `?${query}` : ''}${hash}`;
}

/** Slug explícito (URL, subdomínio ou sessão) — não inclui VITE_DEFAULT_TENANT_SLUG. */
export function resolveExplicitPublicTenantSlug(): string | undefined {
  return getTenantQueryParam() ?? resolveSlugFromHost() ?? readStoredSlug();
}

let resolving: Promise<string | undefined> | null = null;

async function resolvePublicTenant(): Promise<string | undefined> {
  hydratePublicTenantFromStorage();
  const candidates = tenantSlugCandidates({
    hostname: typeof window === 'undefined' ? undefined : window.location.hostname,
    baseDomain: import.meta.env.VITE_BASE_DOMAIN as string | undefined,
    query: getTenantQueryParam(),
    stored: readStoredSlug(),
    fallback: import.meta.env.VITE_DEFAULT_TENANT_SLUG as string | undefined,
  });

  for (const slug of candidates) {
    try {
      const match = await api.resolveTenant(slug);
      const name = match.name?.trim();
      if (name) publicTenantName = name;
      persistTenant(match.id, match.slug);
      return match.id;
    } catch {
      /* tenta o candidato seguinte: query, domínio, valor gravado */
    }
  }

  return publicTenantId;
}

export async function ensurePublicTenantResolved() {
  hydratePublicTenantFromStorage();
  const query = getTenantQueryParam();
  if (publicTenantId && (!query || query === publicTenantSlug)) return publicTenantId;
  if (!resolving) {
    resolving = resolvePublicTenant().finally(() => {
      resolving = null;
    });
  }
  return resolving;
}

export function publicTenantHeaders(): HeadersInit {
  const headers: Record<string, string> = {};
  if (publicTenantId) headers['X-Tenant-Id'] = publicTenantId;
  if (publicTenantSlug) headers['X-Tenant-Slug'] = publicTenantSlug;
  return headers;
}
