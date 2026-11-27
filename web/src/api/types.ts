export type AuthContextOption = {
  kind: 'SuperAdmin' | 'Florist';
  tenantId?: string | null;
  tenantName?: string | null;
  tenantSlug?: string | null;
};

export type AuthResponse = {
  token: string;
  expiresAt: string;
  userId: string;
  email: string;
  role: string;
  tenantId?: string | null;
  tenantSlug?: string | null;
  availableContexts?: AuthContextOption[] | null;
};

export type Tenant = { id: string; name: string; slug: string; description?: string };

export type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };

export type Flower = {
  id: string;
  name: string;
  color?: string;
  unitPrice: number;
  unit?: string;
  stockQuantity?: number | null;
  minimumStockQuantity?: number | null;
  isOutOfStock?: boolean;
  isLowStock?: boolean;
  isApproximatelyAvailable: boolean;
  isCatalogActive?: boolean;
  imageUrl?: string;
  notes?: string;
  vatRate?: number | null;
  category?: string | null;
  seasonMonths?: number[] | null;
};

export type CatalogFlower = Flower & { unit: string; isCatalogActive: boolean };

export type CapacityWindow = {
  label: string;
  start: string;
  end: string;
  limit: number;
  booked: number;
  remaining: number;
};

export type CapacityDay = {
  date: string;
  closed: boolean;
  dailyLimit: number | null;
  booked: number;
  remaining: number | null;
  note?: string | null;
  windows: CapacityWindow[];
};

export type CapacityWindowInput = { label: string; start: string; end: string; orderLimit: number };

export type CapacitySettings = {
  dailyOrderLimit: number | null;
  windows: { label: string; start: string; end: string; orderLimit: number }[];
  exceptions: { date: string; isClosed: boolean; dailyOrderLimitOverride?: number | null; note?: string | null }[];
};

export type StockMovement = {
  id: string;
  flowerId: string;
  delta: number;
  balanceAfter?: number | null;
  reason: number;
  note?: string | null;
  createdAt: string;
};

export type LowStockAlert = {
  flowerId: string;
  name: string;
  stockQuantity: number;
  minimumStockQuantity: number;
};

export type PortfolioArrangement = {
  id: string;
  title: string;
  description?: string;
  price?: number;
  tags?: string;
  status: number;
  sortOrder?: number;
  slug?: string | null;
  photos: { url: string; sortOrder: number }[];
  items: { name: string; quantity: number; isMaterial: boolean; flowerId?: string }[];
};

export type OrderChangeProposal = {
  id: string;
  status: number;
  message: string;
  proposedItemChangesJson?: string;
  revisedTotal?: number;
  createdAt: string;
};

export type OrderStatusHistory = {
  id: string;
  fromStatus: number;
  toStatus: number;
  createdAt: string;
  notes?: string;
};

export type Order = {
  id: string;
  status: number;
  baseType: number;
  deliveryType: number;
  deliveryAddress?: string;
  requestedDeliveryAt?: string;
  customerNotes?: string;
  inspiredByPortfolioArrangementId?: string;
  estimatedTotal: number;
  deliveryFee: number;
  deliveryZoneId?: string;
  deliveryZoneName?: string;
  paymentCaptured: boolean;
  arrangementShape?: number;
  arrangementVariationSeed?: number;
  arrangementPreviewUrl?: string;
  customerName?: string;
  customerEmail?: string;
  createdAt?: string;
  items: { id: string; name: string; quantity: number; unitPrice: number; flowerId?: string }[];
  statusHistory?: OrderStatusHistory[];
  changeProposals?: OrderChangeProposal[];
  payment?: OrderPayment | null;
  customerTaxId?: string | null;
  discountAmount?: number;
  couponCode?: string | null;
  review?: { id: string; stars: number; comment?: string | null; floristReply?: string | null; isHidden: boolean } | null;
  trackingAccessToken?: string | null;
};

export type OrderPayment = {
  id: string;
  status: number;
  amount: number;
  refundedAmount: number;
  currency: string;
};

export const PAYMENT_STATUS_LABELS: Record<number, string> = {
  0: 'Pendente',
  1: 'Pago',
  2: 'Falhado',
  3: 'Reembolsado parcialmente',
  4: 'Reembolsado',
  5: 'Cancelado',
};

export type SalesDocument = {
  id: string;
  orderId: string;
  paymentId: string;
  kind: number;
  status: number;
  documentNumber?: string | null;
  permalink?: string | null;
  grossAmount: number;
  customerTaxId?: string | null;
  lastError?: string | null;
  issuedAt?: string | null;
  createdAt: string;
};

export const INVOICE_KIND_LABELS: Record<number, string> = {
  0: 'Fatura-recibo',
  1: 'Nota de crédito',
};

export const INVOICE_STATUS_LABELS: Record<number, string> = {
  0: 'Pendente',
  1: 'Emitida',
  2: 'Falhada',
};

export type InvoicingSettings = {
  legalName: string;
  taxId: string;
  addressLine: string;
  city: string;
  postalCode: string;
  country: string;
  provider: number;
  accountName?: string | null;
  documentSeries?: string | null;
  defaultVatRate: number;
  taxExemptionCode?: string | null;
  enabled: boolean;
  credentialsConfigured: boolean;
};

export type CheckoutSession = {
  paymentsEnabled: boolean;
  checkoutUrl?: string | null;
  paymentId?: string | null;
  status?: number | null;
  message?: string | null;
};

export type PaymentEvent = {
  id: string;
  eventType: string;
  fromStatus?: number | null;
  toStatus: number;
  amount?: number | null;
  notes?: string | null;
  createdAt: string;
};

export type PaymentRecord = {
  id: string;
  orderId: string;
  status: number;
  amount: number;
  refundedAmount: number;
  commissionPercent: number;
  applicationFeeAmountCents: number;
  currency: string;
  failureMessage?: string | null;
  createdAt: string;
  history: PaymentEvent[];
};

export type StripeConnectStatus = {
  paymentsEnabled: boolean;
  connected: boolean;
  chargesEnabled: boolean;
  payoutsEnabled: boolean;
  detailsSubmitted: boolean;
  cardPayments: boolean;
  mbWay: boolean;
  multibanco: boolean;
  accountId?: string | null;
  publishableKey?: string | null;
};

export type PlatformBilling = {
  commissionPercent: number;
  monthlyFeeAmount: number;
};

export type TenantBilling = {
  commissionPercentOverride?: number | null;
  monthlyFeeAmount?: number | null;
  effectiveCommissionPercent: number;
  effectiveMonthlyFeeAmount: number;
};

export type OpeningHour = {
  dayOfWeek: number;
  openTime?: string;
  closeTime?: string;
  isClosed: boolean;
};

export type DeliveryZone = {
  id: string;
  name: string;
  fee: number;
  isActive: boolean;
  sortOrder: number;
};

export type PublicStoreTheme = {
  name: string;
  slug: string;
  themePrimaryColor: string;
  themeAccentColor: string;
  themeFontFamily?: string;
};

export type PublicStoreCheckout = {
  name: string;
  logoUrl?: string;
  ordersAcceptingEnabled: boolean;
  minimumOrderAdvanceMinutes: number;
  pickupEnabled: boolean;
  homeDeliveryEnabled: boolean;
  cemeteryDeliveryEnabled: boolean;
  openingHours: OpeningHour[];
  deliveryZones: DeliveryZone[];
};

export type OnboardingChecklistItem = {
  key: string;
  title: string;
  description: string;
  isRequired: boolean;
  isComplete: boolean;
  adminPath?: string | null;
  wizardStep?: string | null;
};

export type FloristOnboardingStatus = {
  isPublished: boolean;
  canPublish: boolean;
  missingRequired: string[];
  checklist: OnboardingChecklistItem[];
  currentWizardStep: string;
  wizardProgressPercent: number;
};

export type PlatformTenantOnboarding = {
  tenantId: string;
  name: string;
  slug: string;
  isPublished: boolean;
  floristInviteCompleted: boolean;
  currentWizardStep?: string | null;
  checklist: OnboardingChecklistItem[];
  completedRequiredCount: number;
  requiredCount: number;
};

export type PlatformTenantListItem = {
  id: string;
  name: string;
  slug: string;
  customDomain?: string;
  isActive: boolean;
  isPublished: boolean;
  orderCount: number;
  createdAt: string;
};

export type PlatformTenantDetail = PlatformTenantListItem & {
  contactEmail?: string;
  contactPhone?: string;
  onboardingWizardStep?: string | null;
  themePrimaryColor: string;
  themeAccentColor: string;
  themeFontFamily?: string;
};

export type CreatePlatformTenantResponse = {
  tenant: PlatformTenantDetail;
  floristInviteSent: boolean;
  devInviteLink?: string | null;
};

export type ResendFloristInviteResponse = {
  devInviteLink?: string | null;
};

export type LegalVersions = {
  termsVersion: string;
  privacyVersion: string;
  cookiesVersion: string;
};

export type PublicLegalDocument = {
  documentType: number;
  version: string;
  title: string;
  contentMarkdown: string;
  responsibleSectionMarkdown?: string | null;
};

export type ComplianceAuditEntry = {
  id: string;
  occurredAt: string;
  tenantId?: string | null;
  tenantName?: string | null;
  userId?: string | null;
  action: string;
  details?: string | null;
};

export type StoreSettings = {
  tenantId: string;
  name: string;
  description?: string;
  seoTitle?: string | null;
  seoDescription?: string | null;
  logoUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  addressLine?: string;
  city?: string;
  postalCode?: string;
  customDomain?: string;
  themePrimaryColor: string;
  themeAccentColor: string;
  themeFontFamily?: string;
  ordersAcceptingEnabled: boolean;
  minimumOrderAdvanceMinutes: number;
  pickupEnabled: boolean;
  homeDeliveryEnabled: boolean;
  cemeteryDeliveryEnabled: boolean;
  openingHours: OpeningHour[];
  deliveryZones: DeliveryZone[];
};

export type FloristDashboard = {
  ordersByStatus: Record<string, number>;
  ordersToday: number;
  ordersTomorrow: number;
  monthlyRevenue: number;
  topItems: { name: string; orderLineCount: number; totalValue: number }[];
};

export type FloristDashboardMetrics = {
  days: number;
  pendingOrders: number;
  periodRevenue: number;
  ordersByDay: { date: string; count: number }[];
  revenueByDay: { date: string; revenue: number }[];
  topFlowers: { name: string; orderLineCount: number; totalValue: number }[];
};

export type FloristCustomer = {
  customerId: string;
  fullName?: string;
  email?: string;
  orderCount: number;
  totalSpent: number;
};

export const ORDER_STATUS_LABELS = [
  'Pendente de confirmação',
  'Confirmada',
  'Em preparação',
  'Pronta',
  'Entregue',
  'Cancelada',
  'Recusada',
] as const;

export const DELIVERY_LABELS = ['Levantamento na loja', 'Entrega ao domicílio', 'Cemitério / outro local'] as const;

export const BASE_LABELS = ['Jarra', 'Esponja', 'Embrulho'] as const;

export type PublicSeasonalFlower = {
  id: string;
  name: string;
  color?: string | null;
  category?: string | null;
  unitPrice: number;
  specialPrice?: number | null;
  imageUrl?: string | null;
  seasonMonths: number[];
  isOutOfStock?: boolean;
};

export type PublicCampaign = {
  name: string;
  slug: string;
  description?: string | null;
  startsOn: string;
  endsOn: string;
  flowers: { flowerId: string; name: string; catalogPrice: number; specialPrice?: number | null; isOutOfStock?: boolean }[];
};

export type PublicStorefront = {
  name: string;
  description?: string | null;
  heroImageUrl?: string | null;
  campaign?: PublicCampaign | null;
  seasonalFlowers: PublicSeasonalFlower[];
  averageRating: number;
  reviewCount: number;
};

export type PublicReview = {
  stars: number;
  comment?: string | null;
  floristReply?: string | null;
  createdAt: string;
};

export type Coupon = {
  id: string;
  code: string;
  discountType: number;
  discountValue: number;
  validFrom?: string | null;
  validUntil?: string | null;
  maxRedemptions?: number | null;
  maxRedemptionsPerCustomer?: number | null;
  minimumOrderAmount?: number | null;
  isActive: boolean;
  redemptionCount: number;
  targets: { flowerId?: string | null; category?: string | null }[];
};

export type Campaign = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  startsOn: string;
  endsOn: string;
  highlightOnHome: boolean;
  isActive: boolean;
  flowers: { flowerId: string; name: string; catalogPrice: number; specialPrice?: number | null; sortOrder: number }[];
};

export type FloristReview = {
  id: string;
  orderId: string;
  stars: number;
  comment?: string | null;
  floristReply?: string | null;
  isHidden: boolean;
  isApproved: boolean;
  customerName?: string | null;
  createdAt: string;
};

export type PublicOrderTracking = {
  orderId: string;
  status: number;
  deliveryType: number;
  requestedDeliveryAt?: string | null;
  capacityServiceDate?: string | null;
  deliveryWindowLabel?: string | null;
  deliveryWindowStart?: string | null;
  deliveryWindowEnd?: string | null;
  timeline: { status: number; at: string }[];
};

export type FloristPanelNotification = {
  id: string;
  orderId: string;
  kind: number;
  createdAt: string;
  orderStatus: number;
  estimatedTotal: number;
  shortRef?: string | null;
};

export type FloristPanelNotificationSummary = {
  unreadCount: number;
};

export type RecurringSubscription = {
  id: string;
  status: number;
  frequency: number;
  anchorDeliveryAt: string;
  nextDeliveryAt?: string | null;
  deliveryType: number;
  deliveryAddress?: string;
  deliveryZoneId?: string;
  deliveryZoneName?: string;
  baseType: number;
  isFloristChoice: boolean;
  floristChoiceBudget?: number | null;
  estimatedItemsTotal: number;
  deliveryFee: number;
  customerNotes?: string;
  floristResponseNotes?: string;
  customerName?: string;
  createdAt: string;
  items: { id: string; flowerId?: string; name: string; quantity: number; unitPrice: number }[];
  recentOccurrences: { id: string; scheduledDeliveryAt: string; status: number; generatedOrderId?: string }[];
};

export type FloristUpcomingRecurring = {
  subscriptionId: string;
  customerId?: string;
  customerName?: string;
  scheduledDeliveryAt: string;
  deliveryType: number;
  deliveryAddress?: string;
  isFloristChoice: boolean;
};

export type PlatformMetrics = {
  days: number;
  activeStores: number;
  ordersInPeriod: number;
  gmvInPeriod: number;
  commissionEarned: number;
  storesWithStripeConnected: number;
  newStoresThisMonth: number;
  ordersByDay: { date: string; count: number }[];
  gmvByDay: { date: string; amount: number }[];
  topStores: { tenantId: string; name: string; slug: string; orderCount: number; gmv: number }[];
  newStoresByMonth: { date: string; count: number }[];
};

export const RECURRING_STATUS_LABELS = [
  'À espera da florista',
  'Ativa',
  'Em pausa',
  'Cancelada',
  'Recusada',
] as const;

export const RECURRING_FREQUENCY_LABELS = ['Semanal', 'Quinzenal', 'Mensal'] as const;
