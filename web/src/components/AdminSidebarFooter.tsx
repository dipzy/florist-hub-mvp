import { LogOut, UserRound } from 'lucide-react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { accountProfilePath } from '../lib/authContextsSession';
import { StorePreferencesMenu } from './StorePreferencesMenu';

function emailInitial(email: string): string {
  const ch = email.trim()[0];
  return ch ? ch.toUpperCase() : '?';
}

function truncateEmail(email: string, max = 28): string {
  if (email.length <= max) return email;
  const at = email.indexOf('@');
  if (at > 0 && at < max - 2) {
    return `${email.slice(0, at)}@${email.slice(at + 1, at + 1 + Math.max(4, max - at - 4))}…`;
  }
  return `${email.slice(0, max - 1)}…`;
}

export function AdminSidebarFooter() {
  const auth = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation();

  if (!auth.isAuthenticated) return null;

  const profilePath = accountProfilePath(auth.role);

  function onLogout() {
    auth.logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="admin-sidebar-user">
      <div className="admin-sidebar-user__identity">
        <span className="admin-sidebar-user__avatar" aria-hidden>
          {emailInitial(auth.email ?? '')}
        </span>
        <p className="admin-sidebar-user__email" title={auth.email ?? ''}>
          {truncateEmail(auth.email ?? '')}
        </p>
      </div>
      <div className="admin-sidebar-user__toolbar">
        {profilePath ? (
          <NavLink to={profilePath} className="admin-sidebar-user__action">
            <UserRound size={16} aria-hidden />
            {t('nav.account')}
          </NavLink>
        ) : null}
        <button type="button" className="admin-sidebar-user__action" onClick={onLogout}>
          <LogOut size={16} aria-hidden />
          {t('nav.logout')}
        </button>
        <StorePreferencesMenu className="store-prefs-menu store-prefs-menu--sidebar" placement="sidebar" />
      </div>
    </div>
  );
}
