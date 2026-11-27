import { NavLink, Navigate, Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Building2, Gauge, HandCoins, Shield, type LucideIcon } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { PlatformLogoMark } from './PlatformLogoMark';
import { AdminSidebarFooter } from './AdminSidebarFooter';

const linkDefs: { to: string; labelKey: string; end?: boolean; icon: LucideIcon }[] = [
  { to: '/superadmin', labelKey: 'superadmin.nav.tenants', end: true, icon: Building2 },
  { to: '/superadmin/comissao', labelKey: 'superadmin.nav.billing', icon: HandCoins },
  { to: '/superadmin/metricas', labelKey: 'superadmin.nav.metrics', icon: Gauge },
  { to: '/superadmin/rgpd', labelKey: 'superadmin.nav.gdpr', icon: Shield },
];

export function SuperAdminLayout() {
  const auth = useAuth();
  const { t } = useTranslation();
  if (!auth.isAuthenticated) return <Navigate to="/login" replace />;
  if (auth.role !== 'SuperAdmin') return <Navigate to="/portfolio" replace />;

  return (
    <div className="admin-shell admin-shell--platform">
      <aside className="admin-sidebar" aria-label={t('adminShell.superAdminAria')}>
        <div className="admin-platform-brand">
          <PlatformLogoMark size={36} />
          <span className="admin-platform-brand__text">{t('common.floristHub')}</span>
        </div>
        <p className="admin-brand">{t('adminShell.platform')}</p>
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
                {t(l.labelKey)}
              </NavLink>
            );
          })}
        </nav>
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
