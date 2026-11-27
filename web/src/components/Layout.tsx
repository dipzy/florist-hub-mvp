import { Outlet } from 'react-router-dom';
import { TenantLink, TenantNavLink } from './TenantLink';
import { Flower2, LogIn, Package, Palette, Shield, Sparkles } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { Logo } from './Logo';
import { SessionAccountFooter } from './SessionAccountFooter';
import { StorePreferencesMenu } from './StorePreferencesMenu';

const navLinkClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'active' : undefined);

export function Layout() {
  const auth = useAuth();
  const { t } = useTranslation();

  return (
    <div className="layout">
      <header className="topbar">
        <TenantLink to="/" className="topbar__brand">
          <Logo />
        </TenantLink>
        <nav aria-label={t('nav.ariaMain')}>
          <TenantNavLink to="/" end className={navLinkClass}>
            <Flower2 size={16} aria-hidden />
            {t('nav.store')}
          </TenantNavLink>
          <TenantNavLink to="/portfolio" className={navLinkClass}>
            <Palette size={16} aria-hidden />
            {t('nav.portfolio')}
          </TenantNavLink>
          <TenantNavLink to="/flores" className={navLinkClass}>
            <Flower2 size={16} aria-hidden />
            {t('nav.flowers', { defaultValue: 'Flores' })}
          </TenantNavLink>
          <TenantNavLink to="/montar" className={navLinkClass}>
            <Sparkles size={16} aria-hidden />
            {t('nav.buildArrangement')}
          </TenantNavLink>
          <TenantNavLink to="/encomendas" className={navLinkClass}>
            <Package size={16} aria-hidden />
            {t('nav.orders')}
          </TenantNavLink>
          {auth.role === 'Cliente' && (
            <TenantNavLink to="/recorrencias" className={navLinkClass}>
              <Flower2 size={16} aria-hidden />
              {t('nav.recurring')}
            </TenantNavLink>
          )}
          {auth.role === 'Florista' && (
            <TenantNavLink to="/admin" className={navLinkClass}>
              <Flower2 size={16} aria-hidden />
              {t('nav.floristPanel')}
            </TenantNavLink>
          )}
          {auth.role === 'SuperAdmin' && (
            <TenantNavLink to="/superadmin" className={navLinkClass}>
              <Shield size={16} aria-hidden />
              {t('nav.superAdmin')}
            </TenantNavLink>
          )}
          {auth.isAuthenticated ? (
            <SessionAccountFooter variant="inline" />
          ) : (
            <TenantNavLink to="/login" className={navLinkClass}>
              <LogIn size={16} aria-hidden />
              {t('nav.login')}
            </TenantNavLink>
          )}
        </nav>
        <StorePreferencesMenu className="topbar__preferences store-prefs-menu--topbar" />
      </header>
      <main>
        <Outlet />
      </main>
      <footer className="site-footer muted">
        <TenantLink to="/legal/termos">{t('footer.terms')}</TenantLink>
        <TenantLink to="/legal/privacidade">{t('footer.privacy')}</TenantLink>
        <TenantLink to="/legal/cookies">{t('footer.cookies')}</TenantLink>
        {auth.role === 'Cliente' && <TenantLink to="/privacidade/dados">{t('footer.myData')}</TenantLink>}
      </footer>
    </div>
  );
}
