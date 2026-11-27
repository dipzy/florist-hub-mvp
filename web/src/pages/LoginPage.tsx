import { useEffect, useState, type FormEvent } from 'react';
import { usePageText } from '../lib/usePageText';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { Card } from '../components/ui/Card';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { useStoreBranding } from '../context/StoreBrandingContext';
import { getPublicTenantName, resolveExplicitPublicTenantSlug } from '../lib/publicTenant';
import { readCachedStoreTheme } from '../lib/storeTheme';

function homeForRole(role: string) {
  if (role === 'SuperAdmin') return '/superadmin';
  if (role === 'Florista') return '/admin';
  return '/portfolio';
}

function safeReturnUrl(raw: string | null) {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//')) return null;
  return raw;
}

export function LoginPage() {
  const { t, pt } = usePageText('pages/LoginPage');
  const auth = useAuth();
  const branding = useStoreBranding();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [resolvedName, setResolvedName] = useState<string | null>(
    () => readCachedStoreTheme(resolveExplicitPublicTenantSlug())?.name ?? null,
  );
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const slug = resolveExplicitPublicTenantSlug();
      const cached = readCachedStoreTheme(slug)?.name?.trim();
      if (cached) setResolvedName(cached);
      try {
        const theme = await api.storeTheme();
        const name = theme.name?.trim() || getPublicTenantName()?.trim();
        if (!cancelled && name) setResolvedName(name);
      } catch {
        const name = getPublicTenantName()?.trim();
        if (!cancelled && name) setResolvedName(name);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const storeName = branding.name?.trim() || resolvedName?.trim() || null;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const role = await auth.login(email, password);
      const returnUrl = safeReturnUrl(params.get('returnUrl'));
      navigate(returnUrl ?? homeForRole(role), { replace: true });
    } catch {
      setError(t('auth.login.errorGeneric'));
    }
  }

  const registerTo = params.get('returnUrl')
    ? `/registo?returnUrl=${encodeURIComponent(params.get('returnUrl')!)}`
    : '/registo';

  return (
    <section className="page auth-page">
      <header className="page-header" style={{ textAlign: 'center' }}>
        <h1>{t('auth.login.title')}</h1>
        {storeName?.trim() && <p className="login-store-name">{storeName.trim()}</p>}
        <p className="lede muted">{pt('Use o email e a palavra-passe da sua conta.')}</p>
      </header>
      <Card padding="lg" elevated>
        <form className="form" onSubmit={onSubmit} style={{ maxWidth: 'none' }}>
          <Input
            label={t('auth.login.email')}
            type="email"
            value={email}
            onChange={(ev) => setEmail(ev.target.value)}
            required
            autoComplete="email"
          />
          <Input
            label={t('auth.login.password')}
            type="password"
            value={password}
            onChange={(ev) => setPassword(ev.target.value)}
            required
            autoComplete="current-password"
          />
          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}
          <Button type="submit" variant="primary" size="lg">
            {t('auth.login.submit')}
          </Button>
        </form>
      </Card>
      <p style={{ textAlign: 'center', marginTop: '1.25rem' }}>
        {t('auth.login.noAccount')}{' '}
        <Link to={registerTo}>{t('auth.login.registerLink')}</Link>
      </p>
    </section>
  );
}
