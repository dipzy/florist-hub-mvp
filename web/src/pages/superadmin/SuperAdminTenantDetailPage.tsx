import { useEffect, useState, type FormEvent } from 'react';
import { Tabs } from '../../components/ui/Tabs';
import { ColorField } from '../../components/ui/ColorField';
import { InlineSelect } from '../../components/ui/InlineSelect';
import { usePageText } from '../../lib/usePageText';
import { Link, useParams } from 'react-router-dom';
import { api } from '../../api/client';
import type { PlatformTenantDetail, PlatformTenantOnboarding, TenantBilling } from '../../api/types';
import { OnboardingChecklist } from '../../components/admin/OnboardingChecklist';
import { Button } from '../../components/ui/Button';
import { ErrorState } from '../../components/ui/ErrorState';
import { LoadingState } from '../../components/ui/LoadingState';
import {
  floristInviteResendUnavailableMessage,
  shouldShowFloristInviteResend,
} from '../../lib/floristInviteResend';
import { formatDateTime, formatMoney } from '../../lib/locale';

export function SuperAdminTenantDetailPage() {
  const { pt } = usePageText('pages/superadmin/SuperAdminTenantDetailPage');
  const { id } = useParams<{ id: string }>();
  const [tenant, setTenant] = useState<PlatformTenantDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [devInviteLink, setDevInviteLink] = useState<string | null>(null);
  const [billing, setBilling] = useState<TenantBilling | null>(null);
  const [commission, setCommission] = useState('');
  const [monthly, setMonthly] = useState('');
  const [onboarding, setOnboarding] = useState<PlatformTenantOnboarding | null>(null);
  const [tab, setTab] = useState('identity');

  async function load() {
    if (!id) return;
    const [detail, fees, ob] = await Promise.all([
      api.platformTenant(id),
      api.tenantBilling(id),
      api.platformTenantOnboarding(id),
    ]);
    setTenant(detail);
    setOnboarding(ob);
    setBilling(fees);
    setCommission(fees.commissionPercentOverride == null ? '' : String(fees.commissionPercentOverride));
    setMonthly(fees.monthlyFeeAmount == null ? '' : String(fees.monthlyFeeAmount));
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar.'));
  }, [id]);

  async function save(e: FormEvent) {
    e.preventDefault();
    if (!tenant || !id) return;
    setError(null);
    setMsg(null);
    try {
      const updated = await api.updatePlatformTenant(id, {
        name: tenant.name,
        customDomain: tenant.customDomain,
        themePrimaryColor: tenant.themePrimaryColor,
        themeAccentColor: tenant.themeAccentColor,
        themeFontFamily: tenant.themeFontFamily,
      });
      setTenant(updated);
      setMsg('Alterações guardadas.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao guardar.');
    }
  }

  async function resendInvite() {
    if (!id) return;
    setError(null);
    setMsg(null);
    setDevInviteLink(null);
    try {
      const res = await api.resendFloristInvite(id);
      setMsg('Convite reenviado por email.');
      if (res.devInviteLink) setDevInviteLink(res.devInviteLink);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao reenviar convite.');
    }
  }

  async function toggleSuspend() {
    if (!tenant || !id) return;
    const suspended = tenant.isActive;
    try {
      const updated = await api.suspendPlatformTenant(id, suspended);
      setTenant(updated);
      setMsg(suspended ? 'Loja suspensa.' : 'Loja reativada.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao alterar o estado.');
    }
  }

  if (!tenant) {
    if (error) return <ErrorState message={error} onRetry={() => load().catch(() => undefined)} />;
    return <LoadingState message={pt("A carregar loja…")} />;
  }

  const inviteNote = floristInviteResendUnavailableMessage(onboarding);
  const showResendInvite = shouldShowFloristInviteResend(onboarding);

  return (
    <section className="page admin-tenant-detail">
      <p>
        <Link to="/superadmin">← Voltar à lista</Link>
      </p>
      <header className="page-header">
        <h1>{tenant.name}</h1>
        <p className="muted lede">
          Slug: {tenant.slug} · Encomendas: {tenant.orderCount} · Criada:{' '}
          {formatDateTime(tenant.createdAt)}
        </p>
      </header>
      {!tenant.isActive && <p className="error">Esta loja está suspensa (sem login de florista nem encomendas).</p>}

      <div className="admin-panel-stack">
        {onboarding && (
          <article className="card stack">
            <h2>Estado do onboarding</h2>
            <p className="muted">
              Publicada: {onboarding.isPublished ? 'sim' : 'não'} · Convite concluído:{' '}
              {onboarding.floristInviteCompleted ? 'sim' : 'não'} · Passo: {onboarding.currentWizardStep ?? '—'} ·
              Obrigatórios {onboarding.completedRequiredCount}/{onboarding.requiredCount}
            </p>
            <OnboardingChecklist items={onboarding.checklist} compact />
          </article>
        )}

        <Tabs
          ariaLabel="Secções da loja"
          active={tab}
          onChange={setTab}
          tabs={[
            { id: 'identity', label: 'Identidade' },
            { id: 'colors', label: 'Cores' },
            { id: 'type', label: 'Tipo de letra' },
            { id: 'commission', label: 'Comissão' },
          ]}
        />

        <form className="card form admin-form" onSubmit={save} hidden={tab === 'commission'}>
          <div hidden={tab !== 'identity'}>
            <h2>Identidade</h2>
            <label>
              Nome
              <input value={tenant.name} onChange={(e) => setTenant({ ...tenant, name: e.target.value })} required />
            </label>
            <label>
              Domínio próprio
              <input
                value={tenant.customDomain ?? ''}
                onChange={(e) => setTenant({ ...tenant, customDomain: e.target.value || undefined })}
              />
            </label>
          </div>
          <div hidden={tab !== 'colors'}>
            <h2>Cores</h2>
            <ColorField
              label="Cor principal"
              value={tenant.themePrimaryColor}
              onChange={(themePrimaryColor) => setTenant({ ...tenant, themePrimaryColor })}
            />
            <ColorField
              label="Cor de destaque"
              value={tenant.themeAccentColor}
              onChange={(themeAccentColor) => setTenant({ ...tenant, themeAccentColor })}
            />
          </div>
          <div hidden={tab !== 'type'}>
            <h2>Tipo de letra</h2>
            <InlineSelect
              label="Tipo de letra"
              value={tenant.themeFontFamily ?? 'system'}
              onChange={(e) => setTenant({ ...tenant, themeFontFamily: e.target.value })}
            >
              <option value="system">Sistema</option>
              <option value="serif">Serif</option>
              <option value="rounded">Arredondada</option>
              <option value="classic">Clássica</option>
            </InlineSelect>
          </div>
          {tab !== 'commission' && (
            <Button type="submit" variant="primary">
              Guardar
            </Button>
          )}
        </form>

        <article className="card stack admin-actions-card danger-zone">
          <h2>Ações</h2>
          {inviteNote && <p className="muted">{inviteNote}</p>}
          <div className="toolbar toolbar--cluster">
            {showResendInvite && (
              <Button type="button" variant="secondary" onClick={() => void resendInvite()}>
                Reenviar convite à florista
              </Button>
            )}
            <Button
              type="button"
              variant={tenant.isActive ? 'danger' : 'primary'}
              onClick={() => void toggleSuspend()}
            >
              {tenant.isActive ? 'Suspender loja' : 'Reativar loja'}
            </Button>
          </div>
          {devInviteLink && (
            <p className="notice">
              Link de convite (dev): <a href={devInviteLink}>{devInviteLink}</a>
            </p>
          )}
        </article>

        {billing && tab === 'commission' && (
          <form
            className="card form admin-form"
            onSubmit={async (e) => {
              e.preventDefault();
              if (!id) return;
              setError(null);
              setMsg(null);
              try {
                const saved = await api.updateTenantBilling(
                  id,
                  commission.trim() === '' ? null : Number(commission.replace(',', '.')),
                  monthly.trim() === '' ? null : Number(monthly.replace(',', '.')),
                );
                setBilling(saved);
                setMsg('Comissão da loja guardada. Vazio usa o valor global. A mensalidade não é cobrada.');
              } catch (err) {
                setError(err instanceof Error ? err.message : 'Falha ao guardar a comissão.');
              }
            }}
          >
            <h2>Comissão desta loja</h2>
            <p className="muted">
              Em vigor: {billing.effectiveCommissionPercent}% · mensalidade prevista{' '}
              {formatMoney(billing.effectiveMonthlyFeeAmount)}
            </p>
            <label>
              Comissão própria (%)
              <input value={commission} onChange={(e) => setCommission(e.target.value)} placeholder="Usar a global" />
            </label>
            <label>
              Mensalidade própria (€)
              <input value={monthly} onChange={(e) => setMonthly(e.target.value)} placeholder="Usar a global" />
            </label>
            <Button type="submit" variant="primary">
              Guardar comissão
            </Button>
          </form>
        )}
      </div>

      {msg && <p className="success">{msg}</p>}
      {error && <p className="error">{error}</p>}
    </section>
  );
}
