import { usePageText } from '../lib/usePageText';
import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api, sessionFromAuth } from '../api/client';
import type { LegalVersions } from '../api/types';
import { LegalAcceptanceFields } from '../components/LegalAcceptanceFields';
import { buildLegalAcceptance } from '../lib/legalConsent';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { Input } from '../components/ui/Input';
import { Select } from '../components/ui/Select';
import { ensurePublicTenantResolved, getPublicTenantId } from '../lib/publicTenant';
import { useAuth } from '../context/AuthContext';

function safeReturnUrl(raw: string | null) {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//')) return null;
  return raw;
}

export function RegisterPage() {
  const { pt } = usePageText('pages/RegisterPage');
  const auth = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [role, setRole] = useState<'Cliente' | 'Florista'>('Cliente');
  const [storeTenantId, setStoreTenantId] = useState('');
  const [newTenantName, setNewTenantName] = useState('');
  const [newTenantSlug, setNewTenantSlug] = useState('');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [legalVersions, setLegalVersions] = useState<LegalVersions | null>(null);
  const [acceptLegal, setAcceptLegal] = useState(false);
  const [marketingConsent, setMarketingConsent] = useState(false);

  useEffect(() => {
    (async () => {
      await ensurePublicTenantResolved();
      setStoreTenantId(getPublicTenantId() ?? '');
    })();
    api.legalVersions().then(setLegalVersions).catch(() => setLegalVersions(null));
  }, []);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      if (!legalVersions || !acceptLegal) {
        setError('Tem de aceitar os Termos e a Política de Privacidade.');
        return;
      }
      if (role === 'Cliente' && !storeTenantId) {
        setError(pt("Não foi possível identificar a loja. Abra o site da florista e tente de novo."));
        return;
      }
      const legal = buildLegalAcceptance(legalVersions, acceptLegal);
      const res = await api.register({
        email,
        password,
        fullName,
        role,
        tenantId: role === 'Cliente' ? storeTenantId : undefined,
        newTenantName: role === 'Florista' ? newTenantName : undefined,
        newTenantSlug: role === 'Florista' ? newTenantSlug : undefined,
        acceptTermsAndPrivacy: legal.acceptTermsAndPrivacy,
        marketingConsent,
        termsVersion: legal.termsVersion,
        privacyVersion: legal.privacyVersion,
      });
      auth.adoptSession(sessionFromAuth(res));
      const returnUrl = safeReturnUrl(params.get('returnUrl'));
      if (returnUrl) navigate(returnUrl, { replace: true });
      else navigate(role === 'Florista' ? '/admin' : '/portfolio', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha no registo');
    }
  }

  return (
    <section className="page auth-page">
      <header className="page-header" style={{ textAlign: 'center' }}>
        <h1>Criar conta</h1>
        <p className="lede muted">Cliente nesta loja ou nova loja de florista.</p>
      </header>
      <Card padding="lg" elevated>
        <form className="form" onSubmit={onSubmit} style={{ maxWidth: 'none' }}>
          <Select label="Perfil" value={role} onChange={(ev) => setRole(ev.target.value as 'Cliente' | 'Florista')}>
            <option value="Cliente">Cliente</option>
            <option value="Florista">Florista (nova loja)</option>
          </Select>

          {role === 'Florista' ? (
            <>
              <Input
                label="Nome da loja"
                value={newTenantName}
                onChange={(ev) => setNewTenantName(ev.target.value)}
                required
              />
              <Input
                label="Slug (subdomínio)"
                value={newTenantSlug}
                onChange={(ev) => setNewTenantSlug(ev.target.value)}
                required
              />
            </>
          ) : (
            <p className="muted">A conta ficará associada à loja que está a visitar.</p>
          )}
          <Input label="Nome completo" value={fullName} onChange={(ev) => setFullName(ev.target.value)} required />
          <Input
            label="Correio eletrónico"
            type="email"
            value={email}
            onChange={(ev) => setEmail(ev.target.value)}
            required
            autoComplete="email"
          />
          <Input
            label="Palavra-passe"
            type="password"
            value={password}
            onChange={(ev) => setPassword(ev.target.value)}
            required
            autoComplete="new-password"
          />
          <LegalAcceptanceFields versions={legalVersions} checked={acceptLegal} onCheckedChange={setAcceptLegal} />
          <label className="checkbox-row">
            <input type="checkbox" checked={marketingConsent} onChange={(e) => setMarketingConsent(e.target.checked)} />
            Aceito receber novidades por email (opcional)
          </label>
          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}
          <Button type="submit" variant="primary" size="lg">
            Registar
          </Button>
        </form>
      </Card>
      <p style={{ textAlign: 'center', marginTop: '1.25rem' }}>
        Já tem conta? <Link to={params.get('returnUrl') ? `/login?returnUrl=${encodeURIComponent(params.get('returnUrl')!)}` : '/login'}>Entrar</Link>
      </p>
    </section>
  );
}
