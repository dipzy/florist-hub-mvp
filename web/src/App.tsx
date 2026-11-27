import { lazy, Suspense } from 'react';
import { useTranslation } from 'react-i18next';
import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { Layout } from './components/Layout';
import { AdminLayout } from './components/AdminLayout';
import { AuthProvider, useAuth } from './context/AuthContext';
import { StoreBrandingProvider } from './context/StoreBrandingContext';
import { ToastProvider } from './context/ToastContext';
import { BuildArrangementPage } from './pages/BuildArrangementPage';
import { LoginPage } from './pages/LoginPage';
import { MyOrdersPage } from './pages/MyOrdersPage';
import { OrderDetailPage } from './pages/OrderDetailPage';
import { OrderTrackingPage } from './pages/OrderTrackingPage';
import { PaymentResultPage } from './pages/PaymentResultPage';
import { PortfolioPage } from './pages/PortfolioPage';
import { CampaignPage, PortfolioDetailPage, SeasonalFlowersPage, StoreHomePage } from './pages/StorePublicPages';
import { StoreFlowersPage } from './pages/StoreFlowersPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { RegisterPage } from './pages/RegisterPage';
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { AdminStoreSettingsPage } from './pages/admin/AdminStoreSettingsPage';
import { AdminCatalogPage } from './pages/admin/AdminCatalogPage';
import { AdminCapacityPage } from './pages/admin/AdminCapacityPage';
import { AdminPortfolioPage } from './pages/admin/AdminPortfolioPage';
import { AdminOrdersPage } from './pages/admin/AdminOrdersPage';
import { AdminPaymentsPage } from './pages/admin/AdminPaymentsPage';
import { AdminInvoicesPage } from './pages/admin/AdminInvoicesPage';
import { AdminCustomersPage } from './pages/admin/AdminCustomersPage';
import { AdminCampaignsPage, AdminCouponsPage, AdminReviewsPage } from './pages/admin/AdminMarketingPages';
import { SuperAdminLayout } from './components/SuperAdminLayout';
import { SuperAdminTenantsPage } from './pages/superadmin/SuperAdminTenantsPage';
import { SuperAdminTenantDetailPage } from './pages/superadmin/SuperAdminTenantDetailPage';
import { SuperAdminBillingPage } from './pages/superadmin/SuperAdminBillingPage';
import { SuperAdminMetricsPage } from './pages/superadmin/SuperAdminMetricsPage';
import { RecurringSubscriptionsPage } from './pages/RecurringSubscriptionsPage';
import { AdminRecurringPage } from './pages/admin/AdminRecurringPage';
import { FloristInvitePage } from './pages/FloristInvitePage';
import { LegalDocumentPage } from './pages/LegalDocumentPage';
import { PrivacySettingsPage } from './pages/PrivacySettingsPage';
import { AdminCompliancePage } from './pages/admin/AdminCompliancePage';
import { AdminOnboardingWizardPage } from './pages/admin/AdminOnboardingWizardPage';
import { SuperAdminCompliancePage } from './pages/superadmin/SuperAdminCompliancePage';
import { AccountProfilePage } from './pages/AccountProfilePage';
import { PublicStoreShell } from './components/PublicStoreShell';
import { CookieBanner } from './components/CookieBanner';
import './App.css';

const EmbedConfiguratorPage = lazy(() =>
  import('./pages/EmbedConfiguratorPage').then((module) => ({ default: module.EmbedConfiguratorPage })),
);

function Protected({ children, roles }: { children: React.ReactNode; roles?: string[] }) {
  const auth = useAuth();
  const location = useLocation();
  if (!auth.isAuthenticated) {
    const returnUrl = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />;
  }
  if (roles && auth.role && !roles.includes(auth.role)) return <Navigate to="/portfolio" replace />;
  return children;
}

function EmbedConfiguratorFallback() {
  const { t } = useTranslation();
  return <p className="muted">{t('app.embedConfiguratorLoading')}</p>;
}

export default function App() {
  return (
    <AuthProvider>
      <StoreBrandingProvider>
        <ToastProvider>
          <BrowserRouter>
        <Routes>
          <Route
            path="embed/configurador"
            element={
              <Suspense fallback={<EmbedConfiguratorFallback />}>
                <EmbedConfiguratorPage />
              </Suspense>
            }
          />
          <Route path="/superadmin" element={<SuperAdminLayout />}>
            <Route index element={<SuperAdminTenantsPage />} />
            <Route path="lojas/:id" element={<SuperAdminTenantDetailPage />} />
            <Route path="comissao" element={<SuperAdminBillingPage />} />
            <Route path="metricas" element={<SuperAdminMetricsPage />} />
            <Route path="rgpd" element={<SuperAdminCompliancePage />} />
            <Route path="perfil" element={<AccountProfilePage />} />
          </Route>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<AdminDashboardPage />} />
            <Route path="configuracao-inicial" element={<AdminOnboardingWizardPage />} />
            <Route path="definicoes" element={<AdminStoreSettingsPage />} />
            <Route path="catalogo" element={<AdminCatalogPage />} />
            <Route path="capacidade" element={<AdminCapacityPage />} />
            <Route path="portfolio" element={<AdminPortfolioPage />} />
            <Route path="encomendas" element={<AdminOrdersPage />} />
            <Route path="pagamentos" element={<AdminPaymentsPage />} />
            <Route path="faturacao" element={<AdminInvoicesPage />} />
            <Route path="clientes" element={<AdminCustomersPage />} />
            <Route path="cupoes" element={<AdminCouponsPage />} />
            <Route path="campanhas" element={<AdminCampaignsPage />} />
            <Route path="avaliacoes" element={<AdminReviewsPage />} />
            <Route path="recorrencias" element={<AdminRecurringPage />} />
            <Route path="rgpd" element={<AdminCompliancePage />} />
            <Route path="perfil" element={<AccountProfilePage />} />
          </Route>
          <Route
            element={
              <PublicStoreShell>
                <Layout />
              </PublicStoreShell>
            }
          >
            <Route index element={<StoreHomePage />} />
            <Route path="flores-da-epoca" element={<SeasonalFlowersPage />} />
            <Route path="campanha/:slug" element={<CampaignPage />} />
            <Route path="portfolio/:slug" element={<PortfolioDetailPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="convite" element={<FloristInvitePage />} />
            <Route path="registo" element={<RegisterPage />} />
            <Route path="legal/:doc" element={<LegalDocumentPage />} />
            <Route
              path="privacidade/dados"
              element={
                <Protected roles={['Cliente']}>
                  <PrivacySettingsPage />
                </Protected>
              }
            />
            <Route path="portfolio" element={<PortfolioPage />} />
            <Route path="flores" element={<StoreFlowersPage />} />
            <Route path="florista" element={<Navigate to="/admin" replace />} />
            <Route path="montar" element={<BuildArrangementPage />} />
            <Route path="acompanhar/:token" element={<OrderTrackingPage />} />
            <Route
              path="encomendas"
              element={
                <Protected roles={['Cliente']}>
                  <MyOrdersPage />
                </Protected>
              }
            />
            <Route
              path="recorrencias"
              element={
                <Protected roles={['Cliente']}>
                  <RecurringSubscriptionsPage />
                </Protected>
              }
            />
            <Route
              path="encomendas/:id"
              element={
                <Protected roles={['Cliente']}>
                  <OrderDetailPage />
                </Protected>
              }
            />
            <Route
              path="encomendas/:id/pagamento/sucesso"
              element={
                <Protected roles={['Cliente']}>
                  <PaymentResultPage outcome="sucesso" />
                </Protected>
              }
            />
            <Route
              path="encomendas/:id/pagamento/cancelado"
              element={
                <Protected roles={['Cliente']}>
                  <PaymentResultPage outcome="cancelado" />
                </Protected>
              }
            />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
            <CookieBanner />
          </BrowserRouter>
        </ToastProvider>
      </StoreBrandingProvider>
    </AuthProvider>
  );
}
