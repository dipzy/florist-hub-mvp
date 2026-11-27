import type {
  AuthContextOption,
  AuthResponse,
  CapacitySettings,
  CapacityWindowInput,
  CatalogFlower,
  CreatePlatformTenantResponse,
  FloristCustomer,
  FloristDashboard,
  FloristDashboardMetrics,
  FloristOnboardingStatus,
  Flower,
  CheckoutSession,
  ComplianceAuditEntry,
  LegalVersions,
  LowStockAlert,
  Order,
  PublicLegalDocument,
  Paged,
  PaymentRecord,
  InvoicingSettings,
  SalesDocument,
  PlatformBilling,
  StripeConnectStatus,
  TenantBilling,
  PlatformTenantDetail,
  PlatformTenantListItem,
  PlatformTenantOnboarding,
  PlatformMetrics,
  RecurringSubscription,
  FloristUpcomingRecurring,
  ResendFloristInviteResponse,
  PortfolioArrangement,
  PublicStoreCheckout,
  PublicStoreTheme,
  StockMovement,
  StoreSettings,
  Tenant,
  PublicStorefront,
  PublicSeasonalFlower,
  PublicCampaign,
  PublicReview,
  Coupon,
  Campaign,
  FloristReview,
  PublicOrderTracking,
  FloristPanelNotification,
  FloristPanelNotificationSummary,
} from './types';
import { defaultRequestDeniedFallback, parseApiErrorBody } from '../lib/apiError';
import { getApiLocale } from '../lib/locale';
import { ensurePublicTenantResolved, hydratePublicTenantFromStorage, publicTenantHeaders } from '../lib/publicTenant';
import { ApiRequestError, retryAsync } from '../lib/retry';
import { coerceCapacityDays, coerceCapacitySettings, coerceFlower, coerceOrder, coerceOrders, coercePaged } from '../lib/coerceApi';

const API_BASE =
  import.meta.env.VITE_API_BASE_URL ??
  import.meta.env.VITE_API_URL ??
  'http://localhost:5183';

export type Session = {
  token: string;
  tenantId?: string;
  tenantSlug?: string;
  role: string;
  email: string;
  availableContexts?: AuthContextOption[];
};

export function sessionFromAuth(res: AuthResponse): Session {
  return {
    token: res.token,
    tenantId: res.tenantId ?? undefined,
    tenantSlug: res.tenantSlug ?? undefined,
    role: res.role,
    email: res.email,
    availableContexts: res.availableContexts ?? undefined,
  };
}

let session: Session | null = null;

export function loadSessionFromStorage() {
  const raw = localStorage.getItem('floristhub.session');
  session = raw ? JSON.parse(raw) : null;
}

export function saveSession(s: Session | null) {
  session = s;
  if (s) localStorage.setItem('floristhub.session', JSON.stringify(s));
  else localStorage.removeItem('floristhub.session');
}

export function getSession() {
  return session;
}

function authHeaders(contentTypeJson = true) {
  const headers = new Headers();
  if (contentTypeJson) headers.set('Content-Type', 'application/json');
  if (session?.tenantId) headers.set('X-Tenant-Id', session.tenantId);
  if (session?.token) headers.set('Authorization', `Bearer ${session.token}`);
  headers.set('Accept-Language', getApiLocale());
  return headers;
}

const pathsWithoutPublicTenant = new Set<string>();

function skipPublicTenantResolution(path: string) {
  return pathsWithoutPublicTenant.has(path) || path.startsWith('/api/tenants/resolve/');
}

const ATTEMPT_TIMEOUT_MS = 20_000;

function attemptSignal(external: AbortSignal | null | undefined) {
  const timeout = AbortSignal.timeout(ATTEMPT_TIMEOUT_MS);
  return external ? AbortSignal.any([external, timeout]) : timeout;
}

async function requestOnce<T>(path: string, init: RequestInit = {}): Promise<T> {
  if (!session?.tenantId && !skipPublicTenantResolution(path)) await ensurePublicTenantResolved();
  else hydratePublicTenantFromStorage();
  const headers = authHeaders(!(init.body instanceof FormData));
  // Sessão de florista/cliente: o tenant vem do JWT — não sobrescrever com o tenant público (?tenant=).
  if (session?.tenantId) {
    if (session.tenantSlug) headers.set('X-Tenant-Slug', session.tenantSlug);
  } else {
    new Headers(publicTenantHeaders()).forEach((v, k) => headers.set(k, v));
  }
  new Headers(init.headers).forEach((v, k) => headers.set(k, v));

  const res = await fetch(`${API_BASE}${path}`, { ...init, headers, signal: attemptSignal(init.signal) });
  if (!res.ok) {
    const text = await res.text();
    throw new ApiRequestError(parseApiErrorBody(text, res.statusText || defaultRequestDeniedFallback()), res.status);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase();
  if (method !== 'GET' && method !== 'HEAD') return requestOnce<T>(path, init);
  return retryAsync(() => requestOnce<T>(path, init), { attempts: 3 });
}

export const api = {
  resolveTenant: (slug: string) => request<Tenant>(`/api/tenants/resolve/${encodeURIComponent(slug)}`),
  login: (body: { email: string; password: string; tenantId?: string; tenantSlug?: string }) =>
    request<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  loginWithoutTenant: (body: { email: string; password: string }) =>
    request<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  authContexts: () => request<AuthContextOption[]>('/api/auth/contexts'),
  switchAuthContext: (body: { kind: string; tenantId?: string }) =>
    request<AuthResponse>('/api/auth/switch-context', { method: 'POST', body: JSON.stringify(body) }),
  register: (body: Record<string, unknown>) =>
    request<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  flowers: () => request<Flower[]>('/api/flowers'),
  portfolio: () => request<PortfolioArrangement[]>('/api/portfolio'),
  portfolioManage: () => request<PortfolioArrangement[]>('/api/portfolio/manage'),
  myOrders: () => request<Order[]>('/api/orders/mine'),
  pendingOrders: () => request<Order[]>('/api/orders/pending'),
  floristOrders: async (status?: number) => {
    const raw = await request<unknown>(status != null ? `/api/orders/manage?status=${status}` : '/api/orders/manage');
    if (Array.isArray(raw)) return coerceOrders(raw as Order[]);
    return coerceOrders(coercePaged<Order>(raw, (item) => coerceOrder(item as Order)).items);
  },
  floristOrdersPaged: (params: {
    status?: number;
    deliveryType?: number;
    fromDate?: string;
    toDate?: string;
    customerSearch?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const q = new URLSearchParams();
    if (params.status != null) q.set('status', String(params.status));
    if (params.deliveryType != null) q.set('deliveryType', String(params.deliveryType));
    if (params.fromDate) q.set('fromDate', params.fromDate);
    if (params.toDate) q.set('toDate', params.toDate);
    if (params.customerSearch) q.set('customerSearch', params.customerSearch);
    q.set('page', String(params.page ?? 1));
    q.set('pageSize', String(params.pageSize ?? 20));
    return request<unknown>(`/api/orders/manage/paged?${q}`).then((raw) => {
      const page = coercePaged<Order>(raw, (item) => coerceOrder(item as Order));
      return { ...page, items: coerceOrders(page.items) };
    });
  },
  getOrder: async (id: string) => coerceOrder(await request<Order>(`/api/orders/${id}`)),
  getPortfolioItem: (id: string) => request<PortfolioArrangement>(`/api/portfolio/${id}`),
  updateOrderStatus: (id: string, newStatus: number, notes?: string) =>
    request<Order>(`/api/orders/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ newStatus, notes }),
    }),
  cancelOrder: (id: string, notes?: string) =>
    request<Order>(`/api/orders/${id}/cancel`, { method: 'POST', body: JSON.stringify({ notes }) }),
  startCheckout: (
    id: string,
    successUrl: string,
    cancelUrl: string,
    options: {
      customerTaxId?: string;
      acceptTermsAndPrivacy: boolean;
      termsVersion: string;
      privacyVersion: string;
    },
  ) =>
    request<CheckoutSession>(`/api/orders/${id}/payment`, {
      method: 'POST',
      body: JSON.stringify({
        successUrl,
        cancelUrl,
        customerTaxId: options.customerTaxId?.trim() || null,
        acceptTermsAndPrivacy: options.acceptTermsAndPrivacy,
        termsVersion: options.termsVersion,
        privacyVersion: options.privacyVersion,
      }),
    }),
  legalVersions: () => request<LegalVersions>('/api/legal/versions'),
  legalTerms: () => request<PublicLegalDocument>('/api/legal/terms'),
  legalPrivacy: () => request<PublicLegalDocument>('/api/legal/privacy'),
  legalCookies: () => request<PublicLegalDocument>('/api/legal/cookies'),
  exportMyData: () => request<Record<string, unknown>>('/api/privacy/export'),
  anonymizeMyAccount: () => request<void>('/api/privacy/anonymize', { method: 'POST' }),
  complianceAudit: (page = 1) =>
    request<Paged<ComplianceAuditEntry>>(`/api/compliance-audit?page=${page}&pageSize=50`),
  complianceAuditPlatform: (page = 1) =>
    request<Paged<ComplianceAuditEntry>>(`/api/compliance-audit/platform?page=${page}&pageSize=50`),
  createOrder: (body: Record<string, unknown>, idempotencyKey?: string) =>
    request<Order>('/api/orders', {
      method: 'POST',
      body: JSON.stringify(body),
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
    }),
  confirmOrder: (id: string, notes?: string) =>
    request<Order>(`/api/orders/${id}/confirm`, { method: 'POST', body: JSON.stringify({ notes }) }),
  rejectOrder: (id: string, notes?: string) =>
    request<Order>(`/api/orders/${id}/reject`, { method: 'POST', body: JSON.stringify({ notes }) }),
  proposeOrderChanges: (id: string, body: { message: string; proposedItemChangesJson?: string; revisedTotal?: number }) =>
    request<Order>(`/api/orders/${id}/proposals`, { method: 'POST', body: JSON.stringify(body) }),
  acceptProposal: (orderId: string, proposalId: string, notes?: string) =>
    request<Order>(`/api/orders/${orderId}/proposals/${proposalId}/accept`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  rejectProposal: (orderId: string, proposalId: string, notes?: string) =>
    request<Order>(`/api/orders/${orderId}/proposals/${proposalId}/reject`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  uploadPortfolioPhoto: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return request<{ url: string; storagePath: string }>('/api/files/upload', { method: 'POST', body: form });
  },
  createPortfolio: (body: Record<string, unknown>) =>
    request<PortfolioArrangement>('/api/portfolio', { method: 'POST', body: JSON.stringify(body) }),
  updatePortfolio: (id: string, body: Record<string, unknown>) =>
    request<PortfolioArrangement>(`/api/portfolio/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  setPortfolioStatus: (id: string, status: number) =>
    request<PortfolioArrangement>(`/api/portfolio/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }),
  sortPortfolio: (items: { id: string; sortOrder: number }[]) =>
    request<PortfolioArrangement[]>('/api/portfolio/sort-order', { method: 'PUT', body: JSON.stringify({ items }) }),
  catalogManage: (params: { search?: string; catalogActive?: boolean; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params.search) q.set('search', params.search);
    if (params.catalogActive != null) q.set('catalogActive', String(params.catalogActive));
    q.set('page', String(params.page ?? 1));
    q.set('pageSize', String(params.pageSize ?? 20));
    return request<unknown>(`/api/flowers/manage?${q}`).then((raw) => coercePaged(raw, coerceFlower));
  },
  createFlower: (body: Record<string, unknown>) =>
    request<CatalogFlower>('/api/flowers', { method: 'POST', body: JSON.stringify(body) }),
  updateFlower: (id: string, body: Record<string, unknown>) =>
    request<CatalogFlower>(`/api/flowers/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteFlower: (id: string) => request<void>(`/api/flowers/${id}`, { method: 'DELETE' }),
  flowerMovements: (id: string) => request<StockMovement[]>(`/api/flowers/${id}/movements`),
  lowStockAlerts: () => request<LowStockAlert[]>('/api/flowers/alerts'),
  publicCapacity: async (from: string, to: string) =>
    coerceCapacityDays(await request<unknown>(`/api/store/capacity?from=${from}&to=${to}`)),
  floristCapacity: async (from: string, to: string) =>
    coerceCapacityDays(await request<unknown>(`/api/florist/capacity?from=${from}&to=${to}`)),
  capacitySettings: async () => coerceCapacitySettings(await request<unknown>('/api/florist/capacity/settings')),
  updateCapacitySettings: (body: { dailyOrderLimit: number; windows: CapacityWindowInput[] }) =>
    request<CapacitySettings>('/api/florist/capacity/settings', { method: 'PUT', body: JSON.stringify(body) }),
  upsertCapacityException: (body: {
    date: string;
    isClosed: boolean;
    dailyOrderLimitOverride?: number | null;
    note?: string | null;
  }) => request<unknown>('/api/florist/capacity/exceptions', { method: 'PUT', body: JSON.stringify(body) }),
  storeCheckout: () => request<PublicStoreCheckout>('/api/store/checkout'),
  storeTheme: () => request<PublicStoreTheme>('/api/store/theme'),
  platformTenants: () => request<PlatformTenantListItem[]>('/api/platform/tenants'),
  platformTenant: (id: string) => request<PlatformTenantDetail>(`/api/platform/tenants/${id}`),
  createPlatformTenant: (body: Record<string, unknown>) =>
    request<CreatePlatformTenantResponse>('/api/platform/tenants', { method: 'POST', body: JSON.stringify(body) }),
  updatePlatformTenant: (id: string, body: Record<string, unknown>) =>
    request<PlatformTenantDetail>(`/api/platform/tenants/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  suspendPlatformTenant: (id: string, suspended: boolean) =>
    request<PlatformTenantDetail>(`/api/platform/tenants/${id}/suspend`, {
      method: 'POST',
      body: JSON.stringify({ suspended }),
    }),
  floristOnboarding: () => request<FloristOnboardingStatus>('/api/florist/onboarding'),
  setOnboardingWizardStep: (step: string) =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/wizard-step', {
      method: 'PUT',
      body: JSON.stringify({ step }),
    }),
  acknowledgeOnboardingColors: () =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/acknowledge-colors', { method: 'POST', body: '{}' }),
  setOnboardingDailyCapacity: (dailyOrderLimit: number) =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/daily-capacity', {
      method: 'PUT',
      body: JSON.stringify({ dailyOrderLimit }),
    }),
  setOnboardingPaymentsDisabled: (disabled: boolean) =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/payments-disabled', {
      method: 'PUT',
      body: JSON.stringify({ disabled }),
    }),
  acceptOnboardingPlatformTerms: () =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/accept-platform-terms', { method: 'POST', body: '{}' }),
  publishFloristStore: () =>
    request<FloristOnboardingStatus>('/api/florist/onboarding/publish', { method: 'POST', body: '{}' }),
  platformTenantOnboarding: (id: string) =>
    request<PlatformTenantOnboarding>(`/api/platform/tenants/${id}/onboarding`),
  resendFloristInvite: (id: string) =>
    request<ResendFloristInviteResponse>(`/api/platform/tenants/${id}/resend-invite`, {
      method: 'POST',
      body: JSON.stringify({}),
    }),
  completeFloristInvite: (body: { userId: string; token: string; newPassword: string }) =>
    request<void>('/api/auth/florist-invite/complete', { method: 'POST', body: JSON.stringify(body) }),
  storeSettings: () => request<StoreSettings>('/api/florist/store'),
  updateStoreSettings: (body: Record<string, unknown>) =>
    request<StoreSettings>('/api/florist/store', { method: 'PUT', body: JSON.stringify(body) }),
  floristDashboard: () => request<FloristDashboard>('/api/florist/dashboard'),

  floristDashboardMetrics: (days: number) =>
    request<FloristDashboardMetrics>(`/api/florist/dashboard/metrics?days=${days}`),
  floristStripe: () => request<StripeConnectStatus>('/api/florist/stripe'),
  startStripeOnboarding: (returnUrl: string, refreshUrl: string) =>
    request<{ paymentsEnabled: boolean; url?: string | null; message?: string | null }>('/api/florist/stripe/onboarding', {
      method: 'POST',
      body: JSON.stringify({ returnUrl, refreshUrl }),
    }),
  floristPayments: () => request<PaymentRecord[]>('/api/florist/payments'),
  refundPayment: (id: string, amount?: number, reason?: string) =>
    request<PaymentRecord>(`/api/florist/payments/${id}/refund`, {
      method: 'POST',
      body: JSON.stringify({ amount: amount ?? null, reason }),
    }),
  platformBilling: () => request<PlatformBilling>('/api/platform/billing'),
  updatePlatformBilling: (commissionPercent: number, monthlyFeeAmount: number) =>
    request<PlatformBilling>('/api/platform/billing', {
      method: 'PUT',
      body: JSON.stringify({ commissionPercent, monthlyFeeAmount }),
    }),
  tenantBilling: (id: string) => request<TenantBilling>(`/api/platform/tenants/${id}/billing`),
  updateTenantBilling: (id: string, commissionPercentOverride: number | null, monthlyFeeAmount: number | null) =>
    request<TenantBilling>(`/api/platform/tenants/${id}/billing`, {
      method: 'PUT',
      body: JSON.stringify({ commissionPercentOverride, monthlyFeeAmount }),
    }),
  floristCustomers: (params: { search?: string; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params.search) q.set('search', params.search);
    q.set('page', String(params.page ?? 1));
    q.set('pageSize', String(params.pageSize ?? 20));
    return request<Paged<FloristCustomer>>(`/api/florist/customers?${q}`);
  },
  invoicingSettings: () => request<InvoicingSettings>('/api/florist/invoicing'),
  updateInvoicingSettings: (body: Record<string, unknown>) =>
    request<InvoicingSettings>('/api/florist/invoicing', {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  invoices: () => request<SalesDocument[]>('/api/invoices'),
  orderInvoices: (orderId: string) => request<SalesDocument[]>(`/api/invoices/order/${orderId}`),
  retryInvoice: (id: string) =>
    request<SalesDocument>(`/api/invoices/${id}/retry`, { method: 'POST', body: '{}' }),
  storeHome: () => request<PublicStorefront>('/api/store/home'),
  seasonalFlowers: (month?: number) =>
    request<PublicSeasonalFlower[]>(`/api/store/seasonal${month ? `?month=${month}` : ''}`),
  storeCampaign: (slug: string) => request<PublicCampaign>(`/api/store/campaigns/${slug}`),
  storeReviews: () => request<PublicReview[]>('/api/store/reviews'),
  trackOrder: (token: string) => request<PublicOrderTracking>(`/api/store/order-tracking?token=${encodeURIComponent(token)}`),
  floristPanelNotificationSummary: () =>
    request<FloristPanelNotificationSummary>('/api/florist/panel-notifications/summary'),
  floristPanelNotifications: (limit = 20) =>
    request<FloristPanelNotification[]>(`/api/florist/panel-notifications?limit=${limit}`),
  markFloristPanelNotificationsRead: () =>
    request<void>('/api/florist/panel-notifications/mark-read', { method: 'POST', body: '{}' }),
  portfolioBySlug: (slug: string) => request<PortfolioArrangement>(`/api/portfolio/por/${slug}`),
  applyCoupon: (orderId: string, code: string) =>
    request<Order>(`/api/orders/${orderId}/coupon`, { method: 'POST', body: JSON.stringify({ code }) }),
  removeCoupon: (orderId: string) => request<Order>(`/api/orders/${orderId}/coupon`, { method: 'DELETE' }),
  reviewOrder: (orderId: string, stars: number, comment: string) =>
    request<unknown>(`/api/orders/${orderId}/review`, { method: 'POST', body: JSON.stringify({ stars, comment }) }),
  coupons: () => request<Coupon[]>('/api/florist/coupons'),
  saveCoupon: (body: Record<string, unknown>, id?: string) =>
    request<Coupon>(`/api/florist/coupons${id ? `/${id}` : ''}`, {
      method: id ? 'PUT' : 'POST',
      body: JSON.stringify(body),
    }),
  deleteCoupon: (id: string) => request<void>(`/api/florist/coupons/${id}`, { method: 'DELETE' }),
  campaigns: () => request<Campaign[]>('/api/florist/campaigns'),
  saveCampaign: (body: Record<string, unknown>, id?: string) =>
    request<Campaign>(`/api/florist/campaigns${id ? `/${id}` : ''}`, {
      method: id ? 'PUT' : 'POST',
      body: JSON.stringify(body),
    }),
  deleteCampaign: (id: string) => request<void>(`/api/florist/campaigns/${id}`, { method: 'DELETE' }),
  floristReviews: () => request<FloristReview[]>('/api/florist/reviews'),
  replyReview: (id: string, reply: string) =>
    request<unknown>(`/api/florist/reviews/${id}/reply`, { method: 'POST', body: JSON.stringify({ reply }) }),
  hideReview: (id: string, hidden: boolean, reason: string) =>
    request<unknown>(`/api/florist/reviews/${id}/visibility`, { method: 'POST', body: JSON.stringify({ hidden, reason }) }),
  approveReview: (id: string) => request<unknown>(`/api/florist/reviews/${id}/approve`, { method: 'POST', body: '{}' }),
  recurringSubscriptions: () => request<RecurringSubscription[]>('/api/recurring-subscriptions/mine'),
  createRecurringSubscription: (body: Record<string, unknown>) =>
    request<RecurringSubscription>('/api/recurring-subscriptions', { method: 'POST', body: JSON.stringify(body) }),
  pauseRecurring: (id: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/pause`, { method: 'POST', body: '{}' }),
  resumeRecurring: (id: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/resume`, { method: 'POST', body: '{}' }),
  skipRecurringDelivery: (id: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/skip-next`, { method: 'POST', body: '{}' }),
  cancelRecurring: (id: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/cancel`, { method: 'POST', body: '{}' }),
  floristRecurring: (status?: number) => {
    const q = status != null ? `?status=${status}` : '';
    return request<RecurringSubscription[]>(`/api/recurring-subscriptions/manage${q}`);
  },
  floristRecurringUpcoming: (days = 14) =>
    request<FloristUpcomingRecurring[]>(`/api/recurring-subscriptions/upcoming?days=${days}`),
  acceptRecurring: (id: string, notes?: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/accept`, {
      method: 'POST',
      body: JSON.stringify({ notes: notes ?? null }),
    }),
  rejectRecurring: (id: string, notes?: string) =>
    request<RecurringSubscription>(`/api/recurring-subscriptions/${id}/reject`, {
      method: 'POST',
      body: JSON.stringify({ notes: notes ?? null }),
    }),
  platformMetrics: (days: number) => request<PlatformMetrics>(`/api/platform/metrics?days=${days}`),
  downloadInvoicePdf: async (id: string, filename: string) => {
    if (!session?.tenantId) await ensurePublicTenantResolved();
    const headers = authHeaders(false);
    if (session?.tenantId) {
      if (session.tenantSlug) headers.set('X-Tenant-Slug', session.tenantSlug);
    } else {
      new Headers(publicTenantHeaders()).forEach((value, key) => headers.set(key, value));
    }
    const res = await fetch(`${API_BASE}/api/invoices/${id}/pdf`, { headers });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(parseApiErrorBody(text, res.statusText || defaultRequestDeniedFallback()));
    }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  },
};

export function mediaUrl(path: string) {
  if (path.startsWith('http')) return path;
  return `${API_BASE}${path.startsWith('/') ? path : `/${path}`}`;
}

loadSessionFromStorage();
