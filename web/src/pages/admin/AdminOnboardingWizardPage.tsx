import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { usePageText } from '../../lib/usePageText';
import { Link, useNavigate } from 'react-router-dom';
import { api, mediaUrl } from '../../api/client';
import type { FloristOnboardingStatus, PublicLegalDocument, StoreSettings } from '../../api/types';
import { applyStoreTheme } from '../../lib/storeTheme';
import { OnboardingChecklist } from '../../components/admin/OnboardingChecklist';
import { ErrorState } from '../../components/ui/ErrorState';
import { LoadingState } from '../../components/ui/LoadingState';
import { Button } from '../../components/ui/Button';
import { FileField } from '../../components/ui/FileField';

const STEPS: { id: string; title: string }[] = [
  { id: 'loja', title: 'Dados e logótipo' },
  { id: 'cores', title: 'Cores' },
  { id: 'dominio', title: 'Domínio' },
  { id: 'contactos-horario', title: 'Contactos e horário' },
  { id: 'entrega', title: 'Zonas de entrega' },
  { id: 'capacidade', title: 'Capacidade diária' },
  { id: 'catalogo', title: 'Primeiras flores' },
  { id: 'fiscal', title: 'Faturação' },
  { id: 'pagamentos', title: 'Stripe' },
  { id: 'termos', title: 'Termos' },
];

export function AdminOnboardingWizardPage() {
  const { pt } = usePageText('pages/admin/AdminOnboardingWizardPage');
  const navigate = useNavigate();
  const [status, setStatus] = useState<FloristOnboardingStatus | null>(null);
  const [settings, setSettings] = useState<StoreSettings | null>(null);
  const [terms, setTerms] = useState<PublicLegalDocument | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [flowerName, setFlowerName] = useState('');
  const [flowerPrice, setFlowerPrice] = useState('2.50');
  const [capacity, setCapacity] = useState('');

  const step = status?.currentWizardStep ?? 'loja';
  const stepIndex = Math.max(0, STEPS.findIndex((s) => s.id === step));

  const refresh = useCallback(async () => {
    const [onboarding, store] = await Promise.all([api.floristOnboarding(), api.storeSettings()]);
    setStatus(onboarding);
    setSettings(store);
    setCapacity('');
    applyStoreTheme({
      name: store.name,
      slug: '',
      themePrimaryColor: store.themePrimaryColor,
      themeAccentColor: store.themeAccentColor,
      themeFontFamily: store.themeFontFamily,
    });
  }, []);

  useEffect(() => {
    refresh().catch(() => setErr('Não foi possível carregar o assistente.'));
    api
      .legalTerms()
      .then(setTerms)
      .catch(() => undefined);
  }, [refresh]);

  useEffect(() => {
    if (!settings) return;
    applyStoreTheme({
      name: settings.name,
      slug: '',
      themePrimaryColor: settings.themePrimaryColor,
      themeAccentColor: settings.themeAccentColor,
      themeFontFamily: settings.themeFontFamily,
    });
  }, [settings?.themePrimaryColor, settings?.themeAccentColor, settings?.themeFontFamily, settings?.name]);

  const progress = status?.wizardProgressPercent ?? 0;

  async function go(next: string) {
    setErr(null);
    try {
      const res = await api.setOnboardingWizardStep(next);
      setStatus(res);
    } catch (e) {
      setErr(e instanceof Error ? e.message : 'Erro ao avançar.');
    }
  }

  async function saveStore(e: FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setErr(null);
    try {
      const saved = await api.updateStoreSettings({
        name: settings.name,
        description: settings.description,
        logoUrl: settings.logoUrl,
        contactEmail: settings.contactEmail,
        contactPhone: settings.contactPhone,
        addressLine: settings.addressLine,
        city: settings.city,
        postalCode: settings.postalCode,
        ordersAcceptingEnabled: settings.ordersAcceptingEnabled,
        themePrimaryColor: settings.themePrimaryColor,
        themeAccentColor: settings.themeAccentColor,
        themeFontFamily: settings.themeFontFamily,
        minimumOrderAdvanceMinutes: settings.minimumOrderAdvanceMinutes,
        pickupEnabled: settings.pickupEnabled,
        homeDeliveryEnabled: settings.homeDeliveryEnabled,
        cemeteryDeliveryEnabled: settings.cemeteryDeliveryEnabled,
        openingHours: settings.openingHours,
        deliveryZones: settings.deliveryZones.map((z) => ({
          id: z.id && z.id.length > 8 ? z.id : null,
          name: z.name,
          fee: z.fee,
          isActive: z.isActive,
          sortOrder: z.sortOrder,
        })),
      });
      setSettings(saved);
      setMsg('Definições guardadas.');
      await refresh();
    } catch {
      setErr('Não foi possível guardar.');
    }
  }

  async function onLogo(file: File) {
    const res = await api.uploadPortfolioPhoto(file);
    if (!settings) return;
    setSettings({ ...settings, logoUrl: res.url });
  }

  async function acknowledgeColors() {
    const res = await api.acknowledgeOnboardingColors();
    setStatus(res);
    setMsg('Cores confirmadas.');
  }

  async function saveCapacity() {
    const raw = capacity.trim();
    const value = Number(raw);
    if (!raw || !Number.isFinite(value) || value < 1) {
      setErr('Indique o limite diário (mínimo 1).');
      return;
    }
    const res = await api.setOnboardingDailyCapacity(value);
    setStatus(res);
    setMsg('Capacidade e janela padrão (09:00–19:00) guardadas. Ajuste janelas em Capacidade.');
  }

  async function addFlower() {
    const price = Number(flowerPrice.replace(',', '.'));
    if (!flowerName.trim() || !Number.isFinite(price)) {
      setErr('Indique nome e preço da flor.');
      return;
    }
    await api.createFlower({
      name: flowerName.trim(),
      unitPrice: price,
      stockQuantity: 20,
      isCatalogActive: true,
      isApproximatelyAvailable: true,
    });
    setFlowerName('');
    setMsg('Flor criada no catálogo.');
    await refresh();
  }

  async function disablePayments() {
    const res = await api.setOnboardingPaymentsDisabled(true);
    setStatus(res);
    setMsg('Pagamentos online desativados de propósito.');
  }

  async function acceptTerms() {
    const res = await api.acceptOnboardingPlatformTerms();
    setStatus(res);
    setMsg('Termos aceites.');
  }

  async function publish() {
    setErr(null);
    try {
      const res = await api.publishFloristStore();
      setStatus(res);
      setMsg('Loja publicada. Já está visível para clientes.');
    } catch (e) {
      setErr(e instanceof Error ? e.message : 'Ainda não pode publicar.');
    }
  }

  if (!status || !settings) {
    if (err) return <ErrorState message={err} onRetry={() => refresh().catch(() => undefined)} />;
    return <LoadingState message={pt("A carregar assistente…")} />;
  }

  return (
    <div className="page onboarding-wizard">
      <header className="onboarding-header">
        <h1>Configuração da loja</h1>
        <p className="muted">
          Passo {stepIndex + 1} de {STEPS.length}: {STEPS[stepIndex]?.title}
        </p>
        <div className="onboarding-progress" aria-label={`Progresso ${progress}%`}>
          <div className="onboarding-progress-bar" style={{ width: `${progress}%` }} />
        </div>
        {status.isPublished && (
          <p className="success">A sua loja já está publicada. Pode rever ou alterar estes passos quando quiser.</p>
        )}
      </header>

      {msg && (
        <p className="notice card" role="status">
          {msg}
        </p>
      )}
      {err && (
        <p className="error" role="alert">
          {err}
        </p>
      )}

      <div className="row onboarding-layout">
        <article className="card stack onboarding-step-panel">
          {step === 'loja' && (
            <form className="form admin-form" onSubmit={saveStore}>
              <h2>Dados da loja</h2>
              <label>
                Nome
                <input
                  value={settings.name}
                  onChange={(ev) => setSettings({ ...settings, name: ev.target.value })}
                  required
                />
              </label>
              <label>
                Descrição
                <textarea
                  value={settings.description ?? ''}
                  onChange={(ev) => setSettings({ ...settings, description: ev.target.value })}
                  rows={3}
                />
              </label>
              <FileField label="Logótipo" onFile={(file) => file && void onLogo(file)} />
              {settings.logoUrl && (
                <img src={mediaUrl(settings.logoUrl)} alt={`Logótipo de ${settings.name}`} width={96} className="logo-preview" />
              )}
              <label>
                Correio eletrónico
                <input
                  type="email"
                  value={settings.contactEmail ?? ''}
                  onChange={(ev) => setSettings({ ...settings, contactEmail: ev.target.value })}
                />
              </label>
              <label>
                Telefone
                <input
                  value={settings.contactPhone ?? ''}
                  onChange={(ev) => setSettings({ ...settings, contactPhone: ev.target.value })}
                />
              </label>
              <label>
                Morada
                <input
                  value={settings.addressLine ?? ''}
                  onChange={(ev) => setSettings({ ...settings, addressLine: ev.target.value })}
                />
              </label>
              <div className="row">
                <label>
                  Cidade
                  <input value={settings.city ?? ''} onChange={(ev) => setSettings({ ...settings, city: ev.target.value })} />
                </label>
                <label>
                  Código postal
                  <input
                    value={settings.postalCode ?? ''}
                    onChange={(ev) => setSettings({ ...settings, postalCode: ev.target.value })}
                  />
                </label>
              </div>
              <button type="submit">Guardar dados</button>
            </form>
          )}

          {step === 'cores' && (
            <div className="stack">
              <h2>Cores da loja</h2>
              <div className="row">
                <label>
                  Cor principal
                  <input
                    type="color"
                    value={settings.themePrimaryColor}
                    onChange={(ev) => setSettings({ ...settings, themePrimaryColor: ev.target.value })}
                  />
                </label>
                <label>
                  Cor de destaque
                  <input
                    type="color"
                    value={settings.themeAccentColor}
                    onChange={(ev) => setSettings({ ...settings, themeAccentColor: ev.target.value })}
                  />
                </label>
              </div>
              <button type="button" onClick={() => saveStore({ preventDefault: () => undefined } as FormEvent)}>
                Guardar cores
              </button>
              <button type="button" className="secondary" onClick={acknowledgeColors}>
                Confirmar pré-visualização
              </button>
            </div>
          )}

          {step === 'dominio' && (
            <div className="stack">
              <h2>Domínio da loja</h2>
              <p>
                Subdomínio: <strong>{settings.name}</strong> — endereço público{' '}
                <code>{window.location.host}?tenant=slug-da-loja</code> (o slug foi definido pelo SuperAdmin).
              </p>
              <p className="muted">
                Domínio próprio (opcional): crie um registo <strong>CNAME</strong> do seu domínio para o alvo indicado
                pela plataforma e configure o campo em{' '}
                <Link to="/admin/definicoes">Definições da loja</Link> ou peça ao SuperAdmin.
              </p>
              <pre className="dns-hint">
                {`Tipo: CNAME\nNome: loja (ou www)\nValor: <slug>.floristhub.pt`}
              </pre>
            </div>
          )}

          {step === 'contactos-horario' && (
            <div className="stack">
              <h2>Contactos e horário</h2>
              <p className="muted">
                Ajuste contactos e horário de funcionamento em{' '}
                <Link to="/admin/definicoes">Definições da loja</Link>. Pode voltar aqui depois — o assistente
                memoriza o passo.
              </p>
            </div>
          )}

          {step === 'entrega' && (
            <div className="stack">
              <h2>Zonas e custos de entrega</h2>
              <p className="muted">Configure zonas ativas em <Link to="/admin/definicoes">Definições da loja</Link>.</p>
            </div>
          )}

          {step === 'capacidade' && (
            <div className="stack">
              <h2>Capacidade diária e janelas</h2>
              <p className="muted">
                Defina o limite diário; criamos uma janela padrão das 09:00 às 19:00. Para várias janelas ou exceções,
                use <Link to="/admin/capacidade">Capacidade</Link>.
              </p>
              <label>
                Máximo de encomendas por dia
                <input inputMode="numeric" value={capacity} onChange={(ev) => setCapacity(ev.target.value)} />
              </label>
              <button type="button" onClick={saveCapacity}>
                Guardar capacidade
              </button>
            </div>
          )}

          {step === 'catalogo' && (
            <div className="stack">
              <h2>Primeiras flores</h2>
              <p className="muted">
                Adicione pelo menos uma flor ativa com stock. Pode também usar o{' '}
                <Link to="/admin/catalogo">catálogo completo</Link>.
              </p>
              <label>
                Nome
                <input value={flowerName} onChange={(ev) => setFlowerName(ev.target.value)} />
              </label>
              <label>
                Preço (€, com IVA)
                <input value={flowerPrice} onChange={(ev) => setFlowerPrice(ev.target.value)} inputMode="decimal" />
              </label>
              <button type="button" onClick={addFlower}>
                Adicionar flor
              </button>
            </div>
          )}

          {step === 'fiscal' && (
            <div className="stack">
              <h2>Dados fiscais e InvoiceXpress</h2>
              <p className="muted">
                Opcional nesta fase. Configure em <Link to="/admin/faturacao">Faturação</Link> quando estiver pronta.
              </p>
            </div>
          )}

          {step === 'pagamentos' && (
            <div className="stack">
              <h2>Stripe Connect</h2>
              <p className="muted">
                Ligue a conta em <Link to="/admin/pagamentos">Pagamentos</Link> ou desative pagamentos online de
                propósito (aceite encomendas sem checkout Stripe).
              </p>
              <button type="button" className="secondary" onClick={disablePayments}>
                Desativar pagamentos online
              </button>
            </div>
          )}

          {step === 'termos' && (
            <div className="stack">
              <h2>Termos da plataforma</h2>
              {terms && <pre className="legal-preview">{terms.contentMarkdown.slice(0, 1200)}…</pre>}
              <button type="button" onClick={acceptTerms}>
                Aceito os termos da plataforma Florist Hub
              </button>
            </div>
          )}

          <div className="row onboarding-nav">
            <button
              type="button"
              className="secondary"
              disabled={stepIndex === 0}
              onClick={() => go(STEPS[stepIndex - 1].id)}
            >
              Anterior
            </button>
            {stepIndex < STEPS.length - 1 ? (
              <button type="button" onClick={() => go(STEPS[stepIndex + 1].id)}>
                Seguinte
              </button>
            ) : (
              <button type="button" onClick={() => navigate('/admin')}>
                Ir para o painel
              </button>
            )}
          </div>
        </article>

        <aside className="card onboarding-preview">
          <h2>Pré-visualização</h2>
          <div className="store-preview-chip">
            {settings.logoUrl && (
              <img src={mediaUrl(settings.logoUrl)} alt={`Logótipo de ${settings.name}`} width={40} height={40} />
            )}
            <span>{settings.name}</span>
          </div>
          <button type="button" className="preview-button" disabled aria-hidden>
            Ver portfolio
          </button>
          <p className="muted">As cores aplicam-se em tempo real neste painel.</p>
        </aside>
      </div>

      <section className="card stack">
        <h2>Lista de verificação</h2>
        <OnboardingChecklist items={status.checklist} />
        {!status.isPublished && (
          <Button type="button" onClick={publish} disabled={!status.canPublish}>
            Publicar loja
          </Button>
        )}
        {!status.canPublish && !status.isPublished && (
          <p className="muted">Faltam: {status.missingRequired.join(', ')}</p>
        )}
      </section>
    </div>
  );
}

