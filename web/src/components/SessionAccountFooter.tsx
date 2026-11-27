import { LogOut } from 'lucide-react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { accountProfilePath } from '../lib/authContextsSession';
import { Button } from './ui/Button';

type Props = {
  /** Barra compacta para a topbar da loja pública. */
  variant?: 'sidebar' | 'inline';
};

export function SessionAccountFooter({ variant = 'sidebar' }: Props) {
  const auth = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation();

  if (!auth.isAuthenticated) return null;

  const profilePath = accountProfilePath(auth.role);

  function onLogout() {
    auth.logout();
    navigate('/login', { replace: true });
  }

  if (variant === 'inline') {
    return (
      <Button type="button" variant="secondary" size="sm" onClick={onLogout}>
        <LogOut size={16} aria-hidden />
        {t('nav.logout')}
      </Button>
    );
  }

  return (
    <div className="admin-account">
      {profilePath ? (
        <NavLink to={profilePath} className="admin-account__profile-link">
          {t('nav.account')}
        </NavLink>
      ) : (
        <p className="admin-account__label">{t('nav.account')}</p>
      )}
      <p className="admin-account__email" title={auth.email}>
        {auth.email}
      </p>
      <Button type="button" variant="secondary" size="sm" className="admin-account__logout" onClick={onLogout}>
        <LogOut size={16} aria-hidden />
        {t('nav.logout')}
      </Button>
    </div>
  );
}
