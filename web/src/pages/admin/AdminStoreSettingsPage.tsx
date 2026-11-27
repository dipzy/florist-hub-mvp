import { usePageText } from '../../lib/usePageText';
import { useTranslation } from 'react-i18next';
import { useState, type FormEvent } from 'react';
import { api, mediaUrl } from '../../api/client';
import type { OpeningHour, StoreSettings } from '../../api/types';

import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { FileField } from '../../components/ui/FileField';
import { Tabs } from '../../components/ui/Tabs';
import { StoreThemeColorEditor } from '../../components/admin/StoreThemeColorEditor';
import { InlineSelect } from '../../components/ui/InlineSelect';

const WEEKDAYS = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'];

function defaultHours(): OpeningHour[] {
  return Array.from({ length: 7 }, (_, dayOfWeek) => ({
    dayOfWeek,
    openTime: dayOfWeek === 0 ? '' : '09:00',
    closeTime: dayOfWeek === 0 ? '' : '19:00',
    isClosed: dayOfWeek === 0,
  }));
}

type SettingsTab = 'loja' | 'entregas' | 'horario' | 'aparencia';

export function AdminStoreSettingsPage() {
  const { pt } = usePageText('pages/admin/AdminStoreSettingsPage');
  const { t } = useTranslation();
  const [settings, setSettings] = useState<StoreSettings | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [tab, setTab] = useState<SettingsTab>('loja');

  async function load() {
    setSettings(await api.storeSettings());
  }

  const screen = useAdminLoad(() => load(), []);

  async function onLogo(file: File) {
    const res = await api.uploadPortfolioPhoto(file);
    if (!settings) return;
    setSettings({ ...settings, logoUrl: res.url });
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setMsg(null);
    try {
      const saved = await api.updateStoreSettings({
        name: settings.name,
        description: settings.description,
        seoTitle: settings.seoTitle,
        seoDescription: settings.seoDescription,
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
    } catch {
      setMsg(pt("Não foi possível guardar."));
    }
  }

  if (screen.phase !== 'ready' || !settings) {
    return <AdminLoadGate phase={screen.phase === 'error' ? 'error' : 'loading'} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  const hours = settings.openingHours.length ? settings.openingHours : defaultHours();

  return (
    <div className="page settings-page">
      <h1>Definições da loja</h1>
      <Tabs
        ariaLabel={t('admin.settings.tabsAria')}
        active={tab}
        onChange={(id) => setTab(id as SettingsTab)}
        tabs={[
          { id: 'loja', label: t('admin.settings.tabStore') },
          { id: 'entregas', label: t('admin.settings.tabDeliveries') },
          { id: 'horario', label: t('admin.settings.tabHours') },
          { id: 'aparencia', label: t('admin.settings.tabAppearance') },
        ]}
      />
      <form className="card form admin-form" onSubmit={save}>
        <div hidden={tab !== 'loja'}>
        <label>
          Nome
          <input value={settings.name} onChange={(ev) => setSettings({ ...settings, name: ev.target.value })} required />
        </label>
        <label>
          Descrição
          <textarea
            value={settings.description ?? ''}
            onChange={(ev) => setSettings({ ...settings, description: ev.target.value })}
            rows={3}
          />
        </label>
        <details className="form-fold">
          <summary>Motores de busca</summary>
          <div className="form-fold__body">
            <label>
              Título
              <input value={settings.seoTitle ?? ''} onChange={(ev) => setSettings({ ...settings, seoTitle: ev.target.value })} maxLength={70} />
            </label>
            <label>
              Descrição
              <textarea value={settings.seoDescription ?? ''} onChange={(ev) => setSettings({ ...settings, seoDescription: ev.target.value })} rows={2} maxLength={300} />
            </label>
          </div>
        </details>
        <FileField label="Logo" onFile={(file) => file && void onLogo(file)} />
        {settings.logoUrl && <img src={mediaUrl(settings.logoUrl)} alt="" className="logo-preview" width={120} />}
        <label>
          Email
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
        <p className="muted">Faturação e RGPD em <strong>Faturação</strong>.</p>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={settings.ordersAcceptingEnabled}
            onChange={(ev) => setSettings({ ...settings, ordersAcceptingEnabled: ev.target.checked })}
          />
          Aceitar novas encomendas
        </label>
        </div>

        <div hidden={tab !== 'aparencia'}>
          <label>
            Domínio
            <input value={settings.customDomain ?? '—'} readOnly />
          </label>
          <StoreThemeColorEditor
            primary={settings.themePrimaryColor}
            accent={settings.themeAccentColor}
            storeName={settings.name}
            onPrimaryChange={(themePrimaryColor) => setSettings({ ...settings, themePrimaryColor })}
            onAccentChange={(themeAccentColor) => setSettings({ ...settings, themeAccentColor })}
          />
          <label>
            Tipo de letra
            <InlineSelect
              value={settings.themeFontFamily ?? 'system'}
              onChange={(ev) => setSettings({ ...settings, themeFontFamily: ev.target.value })}
            >
              <option value="system">Sistema</option>
              <option value="serif">Serif</option>
              <option value="rounded">Arredondada</option>
              <option value="classic">Clássica</option>
            </InlineSelect>
          </label>
        </div>

        <div hidden={tab !== 'entregas'}>
        <label>
          Antecedência mínima (minutos)
          <input
            type="number"
            min={0}
            value={settings.minimumOrderAdvanceMinutes}
            onChange={(ev) => setSettings({ ...settings, minimumOrderAdvanceMinutes: Number(ev.target.value) })}
          />
        </label>
        <p className="form-section-title">Tipos de entrega</p>
        <div className="row" style={{ flexWrap: 'wrap', gap: 'var(--fh-space-4)' }}>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={settings.pickupEnabled}
              onChange={(ev) => setSettings({ ...settings, pickupEnabled: ev.target.checked })}
            />
            Levantamento na loja
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={settings.homeDeliveryEnabled}
              onChange={(ev) => setSettings({ ...settings, homeDeliveryEnabled: ev.target.checked })}
            />
            Entrega ao domicílio
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={settings.cemeteryDeliveryEnabled}
              onChange={(ev) => setSettings({ ...settings, cemeteryDeliveryEnabled: ev.target.checked })}
            />
            Cemitério / outro local
          </label>
        </div>

        <details className="form-fold" open={settings.deliveryZones.length > 0}>
          <summary>Zonas de entrega</summary>
          <div className="form-fold__body">
        {settings.deliveryZones.map((z, index) => (
          <div key={z.id ?? index} className="zone-row zone-panel">
            <input
              value={z.name}
              placeholder="Nome da zona"
              onChange={(ev) => {
                const deliveryZones = [...settings.deliveryZones];
                deliveryZones[index] = { ...z, name: ev.target.value };
                setSettings({ ...settings, deliveryZones });
              }}
            />
            <input
              type="number"
              step="0.01"
              min={0}
              value={z.fee}
              onChange={(ev) => {
                const deliveryZones = [...settings.deliveryZones];
                deliveryZones[index] = { ...z, fee: Number(ev.target.value) };
                setSettings({ ...settings, deliveryZones });
              }}
            />
            <label className="checkbox">
              <input
                type="checkbox"
                checked={z.isActive}
                onChange={(ev) => {
                  const deliveryZones = [...settings.deliveryZones];
                  deliveryZones[index] = { ...z, isActive: ev.target.checked };
                  setSettings({ ...settings, deliveryZones });
                }}
              />
              Ativa
            </label>
          </div>
        ))}
        <button
          type="button"
          className="secondary"
          onClick={() =>
            setSettings({
              ...settings,
              deliveryZones: [
                ...settings.deliveryZones,
                { id: '', name: '', fee: 0, isActive: true, sortOrder: settings.deliveryZones.length },
              ],
            })
          }
        >
          Adicionar zona
        </button>
          </div>
        </details>
        </div>

        <div hidden={tab !== 'horario'}>
        {hours.map((h) => (
          <div key={h.dayOfWeek} className="hour-row">
            <strong>{WEEKDAYS[h.dayOfWeek]}</strong>
            <label className="checkbox">
              <input
                type="checkbox"
                checked={h.isClosed}
                onChange={(ev) => {
                  const openingHours = hours.map((x) => (x.dayOfWeek === h.dayOfWeek ? { ...x, isClosed: ev.target.checked } : x));
                  setSettings({ ...settings, openingHours });
                }}
              />
              Encerrado
            </label>
            <input
              type="time"
              value={h.openTime ?? ''}
              disabled={h.isClosed}
              onChange={(ev) => {
                const openingHours = hours.map((x) => (x.dayOfWeek === h.dayOfWeek ? { ...x, openTime: ev.target.value } : x));
                setSettings({ ...settings, openingHours });
              }}
            />
            <input
              type="time"
              value={h.closeTime ?? ''}
              disabled={h.isClosed}
              onChange={(ev) => {
                const openingHours = hours.map((x) => (x.dayOfWeek === h.dayOfWeek ? { ...x, closeTime: ev.target.value } : x));
                setSettings({ ...settings, openingHours });
              }}
            />
          </div>
        ))}
        </div>

        <button type="submit">Guardar</button>
        {msg && <p className="muted">{msg}</p>}
      </form>
    </div>
  );
}
