import { usePageText } from '../lib/usePageText';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, mediaUrl } from '../api/client';
import { FileField } from '../components/ui/FileField';
import type { Order, PortfolioArrangement } from '../api/types';
import { deliveryTypeLabel, orderStatusLabel, proposalStatusLabel } from '../lib/enumLabels';
import { formatMoney } from '../lib/locale';
import { useTranslation } from 'react-i18next';

export function FloristDashboardPage() {
  const { t } = useTranslation();
  const { pt } = usePageText('pages/FloristDashboardPage');

  const proposalStatusLabels: Record<number, string> = {
    0: proposalStatusLabel(0, t),
    1: proposalStatusLabel(1, t),
    2: proposalStatusLabel(2, t),
    3: proposalStatusLabel(3, t),
  };

  const floristFilters: { label: string; status?: number }[] = [
    { label: t('florist.filter.pending'), status: 0 },
    { label: t('florist.filter.confirmed'), status: 1 },
    { label: t('florist.filter.inPrep'), status: 2 },
    { label: t('florist.filter.ready'), status: 3 },
    { label: t('florist.filter.delivered'), status: 4 },
    { label: t('florist.filter.all') },
  ];

  const nextFloristStatus: Partial<Record<number, { next: number; label: string }>> = {
    1: { next: 2, label: t('florist.action.markInPrep') },
    2: { next: 3, label: t('florist.action.markReady') },
    3: { next: 4, label: t('florist.action.markDelivered') },
  };
  const [filter, setFilter] = useState<number | undefined>(0);
  const [orders, setOrders] = useState<Order[]>([]);
  const [portfolio, setPortfolio] = useState<PortfolioArrangement[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [photoUrls, setPhotoUrls] = useState<string[]>([]);
  const [proposalMessage, setProposalMessage] = useState<Record<string, string>>({});
  const [rejectReason, setRejectReason] = useState<Record<string, string>>({});
  const [msg, setMsg] = useState<string | null>(null);

  const counts = useMemo(() => {
    const map: Record<number, number> = {};
    for (const o of orders) map[o.status] = (map[o.status] ?? 0) + 1;
    return map;
  }, [orders]);

  async function refresh() {
    const [allOrders, m] = await Promise.all([api.floristOrders(), api.portfolioManage()]);
    setOrders(allOrders);
    setPortfolio(m);
  }

  async function refreshFiltered() {
    const list = await api.floristOrders(filter);
    setOrders((prev) => {
      const merged = new Map(prev.map((o) => [o.id, o]));
      list.forEach((o) => merged.set(o.id, o));
      return Array.from(merged.values());
    });
  }

  useEffect(() => {
    refresh().catch(console.error);
  }, []);

  useEffect(() => {
    refreshFiltered().catch(console.error);
  }, [filter]);

  const visible = filter == null ? orders : orders.filter((o) => o.status === filter);

  async function confirm(id: string) {
    await api.confirmOrder(id, 'Confirmado no dashboard');
    await refresh();
    await refreshFiltered();
  }

  async function reject(id: string) {
    const notes = rejectReason[id]?.trim();
    if (!notes) {
      setMsg('Indique o motivo da recusa.');
      return;
    }
    await api.rejectOrder(id, notes);
    setRejectReason((prev) => ({ ...prev, [id]: '' }));
    await refresh();
    await refreshFiltered();
  }

  async function propose(id: string) {
    const message = proposalMessage[id]?.trim();
    if (!message) return;
    const order = orders.find((o) => o.id === id);
    const itemsJson = order
      ? JSON.stringify(
          order.items.map((i) => ({
            flowerId: i.flowerId ?? null,
            name: i.name,
            quantity: i.quantity,
            unitPrice: i.unitPrice,
          })),
        )
      : undefined;
    await api.proposeOrderChanges(id, {
      message,
      proposedItemChangesJson: itemsJson,
      revisedTotal: order?.estimatedTotal,
    });
    setProposalMessage((prev) => ({ ...prev, [id]: '' }));
    await refresh();
    await refreshFiltered();
  }

  async function advance(id: string, status: number) {
    await api.updateOrderStatus(id, status, 'Atualização no dashboard da florista');
    await refresh();
    await refreshFiltered();
  }

  async function onPhotoSelected(files: File[]) {
    if (files.length === 0) return;
    setMsg(null);
    try {
      const uploaded: string[] = [];
      for (const file of Array.from(files)) {
        const res = await api.uploadPortfolioPhoto(file);
        uploaded.push(res.url);
      }
      setPhotoUrls((prev) => [...prev, ...uploaded]);
    } catch {
      setMsg('Falha ao carregar foto(s).');
    }
  }

  async function createPortfolio(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    try {
      await api.createPortfolio({
        title,
        description,
        photos: photoUrls.map((url, sortOrder) => ({ url, sortOrder })),
        items: [],
      });
      setTitle('');
      setDescription('');
      setPhotoUrls([]);
      setMsg('Arranjo adicionado ao portfolio.');
      await refresh();
    } catch {
      setMsg(pt("Não foi possível criar o arranjo."));
    }
  }

  return (
    <section className="page">
      <h1>Painel da florista</h1>

      <div className="tabs row">
        {floristFilters.map((f) => (
          <button
            key={f.label}
            type="button"
            className={(f.status === undefined && filter === undefined) || f.status === filter ? '' : 'secondary'}
            onClick={() => setFilter(f.status)}
          >
            {f.label}
            {f.status != null && counts[f.status] != null ? ` (${counts[f.status]})` : ''}
          </button>
        ))}
      </div>

      {visible.length === 0 ? (
        <p className="muted">{t('florist.filter.empty')}</p>
      ) : (
        <div className="grid">
          {visible.map((o) => (
            <article key={o.id} className="card">
              <h3>#{o.id.slice(0, 8)}</h3>
              <p>
                {orderStatusLabel(o.status, t)} — {formatMoney(o.estimatedTotal)}
              </p>
              <p className="muted">
                {deliveryTypeLabel(o.deliveryType, t)} {o.deliveryAddress ? `· ${o.deliveryAddress}` : ''}
              </p>
              <ul className="list compact">
                {o.items.map((i) => (
                  <li key={i.id}>
                    {i.name} x{i.quantity}
                  </li>
                ))}
              </ul>
              {(o.changeProposals ?? []).length > 0 && (
                <div className="stack">
                  <strong>Propostas</strong>
                  {o.changeProposals!.map((p) => (
                    <p key={p.id} className="muted">
                      {p.message} — {proposalStatusLabels[p.status] ?? p.status}
                    </p>
                  ))}
                </div>
              )}
              {o.status === 0 && (
                <>
                  <div className="actions row">
                    <button type="button" onClick={() => confirm(o.id)}>
                      Confirmar
                    </button>
                  </div>
                  <label>
                    Motivo da recusa
                    <textarea
                      value={rejectReason[o.id] ?? ''}
                      onChange={(ev) => setRejectReason((prev) => ({ ...prev, [o.id]: ev.target.value }))}
                      rows={2}
                    />
                  </label>
                  <button type="button" className="secondary" onClick={() => reject(o.id)}>
                    Recusar
                  </button>
                  <label>
                    Propor alterações
                    <textarea
                      value={proposalMessage[o.id] ?? ''}
                      onChange={(ev) => setProposalMessage((prev) => ({ ...prev, [o.id]: ev.target.value }))}
                      rows={2}
                      placeholder={pt("Ex.: substituir túlipas por rosas")}
                    />
                  </label>
                  <button type="button" onClick={() => propose(o.id)}>
                    Enviar proposta
                  </button>
                </>
              )}
              {nextFloristStatus[o.status] && (
                <button type="button" onClick={() => advance(o.id, nextFloristStatus[o.status]!.next)}>
                  {nextFloristStatus[o.status]!.label}
                </button>
              )}
              {o.paymentCaptured && <p className="muted">Pagamento recebido.</p>}
            </article>
          ))}
        </div>
      )}

      <h2>Gestão do portfolio</h2>
      <form className="card form" onSubmit={createPortfolio}>
        <label>
          Título
          <input value={title} onChange={(ev) => setTitle(ev.target.value)} required />
        </label>
        <label>
          Descrição
          <textarea value={description} onChange={(ev) => setDescription(ev.target.value)} rows={2} />
        </label>
        <FileField label="Fotos" multiple onFiles={(files) => void onPhotoSelected(files)} />
        {photoUrls.length > 0 && (
          <div className="thumbs">
            {photoUrls.map((url) => (
              <img key={url} src={mediaUrl(url)} alt="" width={96} height={96} />
            ))}
          </div>
        )}
        <button type="submit">Publicar arranjo</button>
        {msg && <p className="muted">{msg}</p>}
      </form>

      <ul className="list">
        {portfolio.map((p) => (
          <li key={p.id}>
            {p.title} — {p.status === 0 ? 'Ativo' : 'Inativo'}
            {p.photos[0] && (
              <img className="thumb-inline" src={mediaUrl(p.photos[0].url)} alt="" width={48} height={48} />
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
