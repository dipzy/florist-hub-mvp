import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { FloristDashboard, FloristPanelNotification, LowStockAlert, Order } from '../../api/types';
import { OnboardingChecklist } from '../../components/admin/OnboardingChecklist';
import { ErrorState } from '../../components/ui/ErrorState';
import { LoadingState } from '../../components/ui/LoadingState';
import type { FloristOnboardingStatus } from '../../api/types';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { formatMoney } from '../../lib/locale';
import { orderStatusLabel } from '../../lib/enumLabels';
import { normalizeOrderStatus } from '../../lib/coerceApi';
import { recentOrderNews } from '../../lib/orderNews';

export function AdminDashboardPage() {
  const { t } = useTranslation();
  const [summary, setSummary] = useState<FloristDashboard | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [alerts, setAlerts] = useState<LowStockAlert[]>([]);
  const [onboarding, setOnboarding] = useState<FloristOnboardingStatus | null>(null);
  const [notifications, setNotifications] = useState<FloristPanelNotification[]>([]);
  const [recentOrders, setRecentOrders] = useState<Order[]>([]);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let cancel = false;
    setErr(null);
    setSummary(null);
    api
      .floristDashboard()
      .then((value) => {
        if (!cancel) setSummary(value);
      })
      .catch(() => {
        if (!cancel) setErr(t('dashboard.loadError'));
      });
    api.lowStockAlerts().then(setAlerts).catch(() => setAlerts([]));
    api
      .floristOnboarding()
      .then(setOnboarding)
      .catch(() => undefined);
    api
      .floristPanelNotifications(8)
      .then(setNotifications)
      .catch(() => setNotifications([]));
    api
      .floristOrders()
      .then(setRecentOrders)
      .catch(() => setRecentOrders([]));
    return () => {
      cancel = true;
    };
  }, [reloadKey, t]);

  if (err) return <ErrorState message={err} onRetry={() => setReloadKey((key) => key + 1)} />;
  if (!summary) return <LoadingState message={t('loading.dashboard')} />;

  const preparingToday = summary.ordersToday;
  const preparingTomorrow = summary.ordersTomorrow;
  const pending = Number(summary.ordersByStatus['0'] ?? 0);
  const confirmed = Number(summary.ordersByStatus['1'] ?? 0);
  const inPrep = Number(summary.ordersByStatus['2'] ?? 0);
  const ready = Number(summary.ordersByStatus['3'] ?? 0);
  const orderNews = recentOrderNews(
    recentOrders.map((order) => ({
      id: order.id,
      status: normalizeOrderStatus(order.status),
      estimatedTotal: order.estimatedTotal,
      createdAt: order.createdAt,
    })),
  );

  return (
    <div className="page">
      <h1>{t('admin.nav.dashboard')}</h1>
      {onboarding && (!onboarding.isPublished || onboarding.checklist.some((c) => c.isRequired && !c.isComplete)) && (
        <article className="card stack">
          <h2>{t('dashboard.storeSetup')}</h2>
          <OnboardingChecklist items={onboarding.checklist} compact />
          <Link to="/admin/configuracao-inicial" className="button-link">
            {t('dashboard.continueSetup')}
          </Link>
        </article>
      )}
      <article className="card stack">
        <div className="row spread">
          <h2 style={{ margin: 0 }}>{t('panelNotifications.heading')}</h2>
          {notifications.length > 0 && (
            <button
              type="button"
              className="fh-btn fh-btn--secondary fh-btn--sm"
              onClick={() =>
                api.markFloristPanelNotificationsRead().then(() => {
                  setNotifications([]);
                })
              }
            >
              {t('panelNotifications.markRead')}
            </button>
          )}
        </div>
        {notifications.length === 0 && orderNews.length === 0 ? (
          <p className="muted">{t('panelNotifications.empty')}</p>
        ) : notifications.length === 0 ? (
          <ul className="list">
            {orderNews.map((order) => (
              <li key={order.id}>
                <Link to="/admin/encomendas" className="fh-btn fh-btn--ghost">
                  {orderStatusLabel(order.status, t)} #{order.id.slice(0, 8)} · {formatMoney(order.estimatedTotal)}
                </Link>
              </li>
            ))}
          </ul>
        ) : (
          <ul className="list">
            {notifications.map((n) => (
              <li key={n.id}>
                <Link to="/admin/encomendas" className="fh-btn fh-btn--ghost">
                  {n.kind === 1 ? t('panelNotifications.paymentReceived') : t('panelNotifications.newOrder')}{' '}
                  {n.shortRef ?? ''} · {formatMoney(n.estimatedTotal)}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </article>
      {alerts.length > 0 && (
        <p className="error-banner">
          {t('dashboard.lowStockPrefix')}{' '}
          {alerts.map((alert) => `${alert.name} (${alert.stockQuantity}/${alert.minimumStockQuantity})`).join(', ')}.
        </p>
      )}
      <div className="grid stats">
        <article className="card stat-card">
          <p className="stat-label">{t('dashboard.pendingOrders', { defaultValue: 'Novas / pendentes' })}</p>
          <p className="stat">{pending}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('dashboard.deliveriesToday')}</p>
          <p className="stat">{preparingToday}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('dashboard.deliveriesTomorrow')}</p>
          <p className="stat">{preparingTomorrow}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">Confirmadas</p>
          <p className="stat">{confirmed}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">Em preparação / prontas</p>
          <p className="stat">
            {inPrep} / {ready}
          </p>
        </article>
      </div>
      <p>
        <Link to="/admin/encomendas">Abrir quadro de encomendas</Link>
      </p>
    </div>
  );
}
