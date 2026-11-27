import { useEffect, useState } from 'react';
import { NavLink, Outlet, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '../api/client';
import { applyStoreTheme } from '../lib/storeTheme';
import {
  CalendarDays,
  Flower2,
  Images,
  LayoutDashboard,
  CreditCard,
  Receipt,
  Package,
  Settings,
  Store,
  Users,
  Shield,
  ListChecks,
  Ticket,
  CalendarHeart,
  Star,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { Logo } from './Logo';
import { AdminSidebarFooter } from './AdminSidebarFooter';

const linkDefs: { to: string; labelKey: string; end?: boolean; icon: LucideIcon }[] = [
  { to: '/admin', labelKey: 'admin.nav.dashboard', end: true, icon: LayoutDashboard },
  { to: '/admin/configuracao-inicial', labelKey: 'admin.nav.onboarding', icon: ListChecks },
  { to: '/admin/definicoes', labelKey: 'admin.nav.settings', icon: Settings },
  { to: '/admin/catalogo', labelKey: 'admin.nav.catalog', icon: Flower2 },
  { to: '/admin/capacidade', labelKey: 'admin.nav.capacity', icon: CalendarDays },
  { to: '/admin/portfolio', labelKey: 'admin.nav.portfolio', icon: Images },
  { to: '/admin/encomendas', labelKey: 'admin.nav.orders', icon: Package },
  { to: '/admin/recorrencias', labelKey: 'admin.nav.recurring', icon: Package },
  { to: '/admin/pagamentos', labelKey: 'admin.nav.payments', icon: CreditCard },
  { to: '/admin/faturacao', labelKey: 'admin.nav.invoicing', icon: Receipt },
  { to: '/admin/clientes', labelKey: 'admin.nav.customers', icon: Users },
  { to: '/admin/cupoes', labelKey: 'admin.nav.coupons', icon: Ticket },
  { to: '/admin/campanhas', labelKey: 'admin.nav.campaigns', icon: CalendarHeart },
  { to: '/admin/avaliacoes', labelKey: 'admin.nav.reviews', icon: Star },
  { to: '/admin/rgpd', labelKey: 'admin.nav.gdpr', icon: Shield },
];

export function AdminLayout() {
  const auth = useAuth();
  const { t } = useTranslation();
  const [unreadOrders, setUnreadOrders] = useState(0);

  useEffect(() => {
    let cancelled = false;
    const refresh = () => {
      api
        .floristPanelNotificationSummary()
        .then((summary) => {
          if (!cancelled) setUnreadOrders(summary.unreadCount);
        })
        .catch(() => {
          if (!cancelled) setUnreadOrders(0);
        });
    };
    refresh();
    const timer = window.setInterval(refresh, 60_000);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    api
      .storeTheme()
      .then((theme) => {
        if (!cancelled) applyStoreTheme(theme);
      })
      .catch(() => {
        if (!cancelled) applyStoreTheme(null);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!auth.isAuthenticated) return <Navigate to="/login" replace />;
  if (auth.role !== 'Florista') return <Navigate to="/portfolio" replace />;

  return (
    <div className="admin-shell">
      <aside className="admin-sidebar" aria-label={t('adminShell.managementMenu')}>
        <Logo compact showWordmark />
        <h2 className="admin-brand">{t('adminShell.storeManagement')}</h2>
        <nav className="admin-nav">
          {linkDefs.map((l) => {
            const Icon = l.icon;
            return (
              <NavLink
                key={l.to}
                to={l.to}
                end={l.end}
                className={({ isActive }) => (isActive ? 'active' : undefined)}
              >
                <Icon size={18} aria-hidden />
                <span className="admin-nav__label">
                  {t(l.labelKey)}
                  {l.to === '/admin/encomendas' && unreadOrders > 0 ? (
                    <span
                      className="admin-nav__badge"
                      aria-label={t('admin.nav.unreadNotifications', { count: unreadOrders })}
                    >
                      {unreadOrders > 99 ? '99+' : unreadOrders}
                    </span>
                  ) : null}
                </span>
              </NavLink>
            );
          })}
        </nav>
        <div className="admin-sidebar__spacer" aria-hidden />
        <NavLink to="/portfolio" className="admin-back">
          <Store size={18} aria-hidden />
          {t('adminShell.viewStoreAsCustomer')}
        </NavLink>
        <div className="admin-sidebar__footer">
          <AdminSidebarFooter />
        </div>
      </aside>
      <section className="admin-main">
        <Outlet />
      </section>
    </div>
  );
}
