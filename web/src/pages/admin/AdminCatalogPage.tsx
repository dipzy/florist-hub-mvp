import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { api, mediaUrl } from '../../api/client';
import type { CatalogFlower, StockMovement } from '../../api/types';
import { catalogOrderableLabel, isCatalogOrderable } from '../../lib/catalogAvailability';
import { formatMoney } from '../../lib/locale';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { Button } from '../../components/ui/Button';
import { FileField } from '../../components/ui/FileField';
import { InlineSelect } from '../../components/ui/InlineSelect';
import { Modal } from '../../components/ui/Modal';

const PAGE_SIZE = 24;

const emptyForm = (): Partial<CatalogFlower> => ({
  name: '',
  unitPrice: 0,
  unit: 'unidade',
  isApproximatelyAvailable: true,
  isCatalogActive: true,
});

export function AdminCatalogPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [items, setItems] = useState<CatalogFlower[]>([]);
  const [form, setForm] = useState<Partial<CatalogFlower> & { id?: string }>(emptyForm());
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);

  async function refresh(p = page) {
    setLoadError(null);
    const res = await api.catalogManage({ search: search || undefined, page: p, pageSize: PAGE_SIZE });
    setItems(res.items);
    setTotal(res.total);
    setPage(res.page);
  }

  const screen = useAdminLoad(() => refresh(1), [search]);

  async function save(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    const body = {
      name: form.name ?? '',
      color: form.color,
      unitPrice: Number(form.unitPrice ?? 0),
      unit: form.unit,
      vatRate: form.vatRate == null ? null : Number(form.vatRate),
      stockQuantity: form.stockQuantity ?? null,
      minimumStockQuantity: form.minimumStockQuantity ?? null,
      isApproximatelyAvailable: form.isApproximatelyAvailable ?? true,
      isCatalogActive: form.isCatalogActive ?? true,
      imageUrl: form.imageUrl,
      notes: form.notes,
      category: form.category || null,
      seasonMonths: (form.seasonMonths ?? []).filter((month) => month >= 1 && month <= 12),
    };
    try {
      if (form.id) await api.updateFlower(form.id, body);
      else await api.createFlower(body);
      setForm(emptyForm());
      setEditorOpen(false);
      setMsg(t('admin.catalog.updated'));
      await refresh();
    } catch {
      setMsg(t('admin.catalog.saveError'));
    }
  }

  async function remove(id: string) {
    await api.deleteFlower(id);
    setMsg(t('admin.catalog.removed'));
    await refresh();
  }

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const [quickName, setQuickName] = useState('');
  const [quickPrice, setQuickPrice] = useState('2.50');
  const [quickStock, setQuickStock] = useState('10');
  const [quickFile, setQuickFile] = useState<File | null>(null);

  async function saveQuickFlower(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    try {
      let imageUrl: string | undefined;
      if (quickFile) {
        imageUrl = (await api.uploadPortfolioPhoto(quickFile)).url;
      }
      await api.createFlower({
        name: quickName.trim(),
        unitPrice: Number(quickPrice),
        stockQuantity: Number(quickStock),
        unit: 'unidade',
        isApproximatelyAvailable: true,
        isCatalogActive: true,
        imageUrl,
      });
      setQuickName('');
      setQuickPrice('2.50');
      setQuickStock('10');
      setQuickFile(null);
      setMsg(t('admin.catalog.created'));
      await refresh();
    } catch {
      setMsg(t('admin.catalog.createError'));
    }
  }

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <header className="catalog-header">
        <h1>{t('admin.catalog.title')}</h1>
        <Button
          className="catalog-header__action"
          onClick={() => {
            setForm(emptyForm());
            setMovements([]);
            setEditorOpen(true);
          }}
        >
          {t('admin.catalog.newItem')}
        </Button>
      </header>
      <form className="card form admin-form" onSubmit={saveQuickFlower}>
        <h2>{t('admin.catalog.quickTitle')}</h2>
        <p className="muted">{t('admin.catalog.quickLede')}</p>
        <FileField label={t('admin.catalog.photo')} onFile={setQuickFile} />
        <label>
          {t('common.name')}
          <input value={quickName} onChange={(ev) => setQuickName(ev.target.value)} required />
        </label>
        <label>
          {t('admin.catalog.price')} (€)
          <input type="number" step="0.01" min={0} value={quickPrice} onChange={(ev) => setQuickPrice(ev.target.value)} required />
        </label>
        <label>
          {t('admin.catalog.stock')}
          <input type="number" min={0} value={quickStock} onChange={(ev) => setQuickStock(ev.target.value)} required />
        </label>
        <button type="submit">{t('admin.catalog.createQuick')}</button>
      </form>
      {loadError && <p className="error" role="alert">{loadError}</p>}
      <div className="row">
        <input placeholder={t('common.search')} value={search} onChange={(ev) => setSearch(ev.target.value)} />
      </div>
      <table className="admin-table">
        <thead>
          <tr>
            <th>{t('common.name')}</th>
            <th>{t('admin.catalog.colPrice')}</th>
            <th>{t('admin.catalog.colVat')}</th>
            <th>{t('admin.catalog.colUnit')}</th>
            <th>{t('admin.catalog.colStock')}</th>
            <th>{t('admin.catalog.colOrderable')}</th>
            <th>{t('admin.catalog.colActive')}</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((f) => (
            <tr key={f.id}>
              <td>
                <div className="catalog-name-cell">
                  {f.imageUrl ? (
                    <img src={mediaUrl(f.imageUrl)} alt="" width={40} height={40} className="thumb-inline catalog-thumb" />
                  ) : (
                    <span className="catalog-thumb catalog-thumb--fallback" aria-hidden>
                      {(f.name || '?').slice(0, 1)}
                    </span>
                  )}
                  {f.name}
                </div>
              </td>
              <td>{formatMoney(f.unitPrice)}</td>
              <td>{f.vatRate == null ? t('admin.catalog.storeVat') : `${f.vatRate}%`}</td>
              <td>{f.unit}</td>
              <td>
                {f.stockQuantity ?? t('common.dash')}
                {f.isLowStock ? ` · ${t('common.lowStock')}` : ''}
                {!isCatalogOrderable(f) && f.isCatalogActive !== false ? ` · ${t('common.outOfStockSuffix')}` : ''}
              </td>
              <td>{catalogOrderableLabel(f, t('common.yes'), t('common.no'))}</td>
              <td>{f.isCatalogActive ? t('common.yes') : t('common.no')}</td>
              <td>
                <div className="catalog-actions">
                <button
                  type="button"
                  className="fh-btn fh-btn--secondary"
                  onClick={() => {
                    setForm({ ...f });
                    setEditorOpen(true);
                    api.flowerMovements(f.id).then(setMovements).catch(() => setMovements([]));
                  }}
                >
                  {t('common.edit')}
                </button>
                <button type="button" className="fh-btn fh-btn--danger fh-btn--secondary" onClick={() => remove(f.id)}>
                  {t('admin.catalog.delete')}
                </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="row">
        <button type="button" className="fh-btn fh-btn--secondary" disabled={page <= 1} onClick={() => refresh(page - 1)}>
          {t('pagination.prev', { defaultValue: 'Anterior' })}
        </button>
        <span className="muted">{t('admin.catalog.pageOf', { page, pages })}</span>
        <button type="button" className="fh-btn fh-btn--secondary" disabled={page >= pages} onClick={() => refresh(page + 1)}>
          {t('pagination.next', { defaultValue: 'Seguinte' })}
        </button>
      </div>

      <Modal
        open={editorOpen}
        title={form.id ? t('admin.catalog.editItem') : t('admin.catalog.newItem')}
        onClose={() => setEditorOpen(false)}
      >
      <form className="form admin-form" onSubmit={save}>
        <label>
          {t('common.name')}
          <input value={form.name ?? ''} onChange={(ev) => setForm({ ...form, name: ev.target.value })} required />
        </label>
        <label>
          {t('build.color', { defaultValue: 'Cor' })}
          <input value={form.color ?? ''} onChange={(ev) => setForm({ ...form, color: ev.target.value })} />
        </label>
        <label>
          {t('admin.catalog.category')}
          <input value={form.category ?? ''} onChange={(ev) => setForm({ ...form, category: ev.target.value })} placeholder="rosas" />
        </label>
        <label>
          {t('admin.catalog.seasonMonths')}
          <input
            value={(form.seasonMonths ?? []).join(',')}
            onChange={(ev) =>
              setForm({
                ...form,
                seasonMonths: ev.target.value
                  .split(',')
                  .map((part) => Number(part.trim()))
                  .filter((month) => month >= 1 && month <= 12),
              })
            }
          />
        </label>
        <label>
          {t('admin.catalog.unitPrice')} (€)
          <input
            type="number"
            step="0.01"
            value={form.unitPrice ?? 0}
            onChange={(ev) => setForm({ ...form, unitPrice: Number(ev.target.value) })}
          />
        </label>
        <label>
          IVA
          <InlineSelect
            value={form.vatRate ?? ''}
            onChange={(ev) =>
              setForm({ ...form, vatRate: ev.target.value === '' ? null : Number(ev.target.value) })
            }
          >
            <option value="">{t('admin.catalog.vatRate')}</option>
            <option value="23">23%</option>
            <option value="22">22%</option>
            <option value="16">16%</option>
            <option value="13">13%</option>
            <option value="6">6%</option>
            <option value="0">0%</option>
          </InlineSelect>
        </label>
        <label>
          {t('admin.catalog.colUnit')}
          <input value={form.unit ?? 'unidade'} onChange={(ev) => setForm({ ...form, unit: ev.target.value })} />
        </label>
        <label>
          {t('admin.catalog.optionalStock')}
          <input
            type="number"
            value={form.stockQuantity ?? ''}
            onChange={(ev) =>
              setForm({ ...form, stockQuantity: ev.target.value === '' ? undefined : Number(ev.target.value) })
            }
          />
        </label>
        <label>
          {t('admin.catalog.minStock')}
          <input
            type="number"
            min={0}
            value={form.minimumStockQuantity ?? ''}
            onChange={(ev) =>
              setForm({ ...form, minimumStockQuantity: ev.target.value === '' ? undefined : Number(ev.target.value) })
            }
          />
        </label>
        <FileField
          label={t('admin.catalog.photo')}
          onFile={async (file) => {
            if (!file) return;
            const res = await api.uploadPortfolioPhoto(file);
            setForm({ ...form, imageUrl: res.url });
          }}
        />
        <label className="checkbox">
          <input
            type="checkbox"
            checked={form.isApproximatelyAvailable ?? true}
            onChange={(ev) => setForm({ ...form, isApproximatelyAvailable: ev.target.checked })}
          />
          {t('admin.catalog.orderableFlag')}
        </label>
        {form.id && (
          <label className="checkbox">
            <input
              type="checkbox"
              checked={form.isCatalogActive ?? true}
              onChange={(ev) => setForm({ ...form, isCatalogActive: ev.target.checked })}
            />
            {t('admin.catalog.activeFlag')}
          </label>
        )}
        <button type="submit">{t('admin.catalog.saveItem')}</button>
        {msg && <p className="muted">{msg}</p>}
        {form.id && movements.length > 0 && (
          <ul className="list">
            {movements.slice(0, 8).map((movement) => (
              <li key={movement.id}>
                {movement.delta > 0 ? `+${movement.delta}` : movement.delta} → {movement.balanceAfter ?? t('common.dash')}
              </li>
            ))}
          </ul>
        )}
      </form>
      </Modal>
    </div>
  );
}
