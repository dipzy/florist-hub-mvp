import { usePageText } from '../../lib/usePageText';
import { useState } from 'react';
import { api, mediaUrl } from '../../api/client';
import type { Order } from '../../api/types';
import {
  deliveryTypeLabel,
  deliveryTypeOptions,
  orderStatusLabel,
  orderStatusOptions,
  paymentStatusLabel,
} from '../../lib/enumLabels';
import { formatMoney } from '../../lib/locale';
import { normalizeOrderStatus } from '../../lib/coerceApi';
import { useTranslation } from 'react-i18next';
import { OrderArrangementPreview } from '../../configurator/OrderArrangementPreview';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { InlineSelect } from '../../components/ui/InlineSelect';

export function AdminOrdersPage() {
  const { t } = useTranslation();
  const { pt } = usePageText('pages/admin/AdminOrdersPage');
  const statusLabels = orderStatusOptions(t);
  const deliveryLabels = deliveryTypeOptions(t);

  const nextFloristStatus: Partial<Record<number, { next: number; label: string }>> = {
    1: { next: 2, label: t('florist.action.markInPrep') },
    2: { next: 3, label: t('florist.action.markReady') },
    3: { next: 4, label: t('florist.action.markDelivered') },
  };

  const BOARD_COLUMNS = [
    { status: 0, title: orderStatusLabel(0, t) },
    { status: 1, title: orderStatusLabel(1, t) },
    { status: 2, title: orderStatusLabel(2, t) },
    { status: 3, title: orderStatusLabel(3, t) },
    { status: 4, title: orderStatusLabel(4, t) },
  ] as const;
  const [status, setStatus] = useState<number | ''>('');
  const [delivery, setDelivery] = useState<number | ''>('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [orders, setOrders] = useState<Order[]>([]);
  const [proposalMessage, setProposalMessage] = useState<Record<string, string>>({});
  const [rejectReason, setRejectReason] = useState<Record<string, string>>({});
  const [openArrangement, setOpenArrangement] = useState<string | null>(null);
  const [boardOrders, setBoardOrders] = useState<Order[]>([]);
  const [listLoading, setListLoading] = useState(true);

  const unfiltered = status === '' && delivery === '' && !search;

  async function refresh(p = page) {
    setListLoading(true);
    try {
      const [allResult, pageResult] = await Promise.allSettled([
        api.floristOrders(),
        api.floristOrdersPaged({
          status: status === '' ? undefined : status,
          deliveryType: delivery === '' ? undefined : delivery,
          customerSearch: search || undefined,
          page: p,
          pageSize: 10,
        }),
      ]);
      if (allResult.status === 'rejected' && pageResult.status === 'rejected') {
        throw allResult.reason instanceof Error ? allResult.reason : new Error('orders');
      }
      const all = allResult.status === 'fulfilled' ? allResult.value : [];
      setBoardOrders(all);
      if (pageResult.status === 'fulfilled') {
        const res = pageResult.value;
        const pagedEmpty = res.total === 0 && res.items.length === 0;
        if (pagedEmpty && unfiltered && all.length > 0) {
          const start = (Math.max(1, p) - 1) * 10;
          setOrders(all.slice(start, start + 10));
          setTotal(all.length);
          setPage(Math.max(1, p));
        } else {
          setOrders(res.items);
          setTotal(res.total);
          setPage(res.page);
        }
      } else if (unfiltered) {
        const start = (Math.max(1, p) - 1) * 10;
        setOrders(all.slice(start, start + 10));
        setTotal(all.length);
        setPage(Math.max(1, p));
      } else {
        setOrders([]);
        setTotal(0);
      }
    } finally {
      setListLoading(false);
    }
  }

  const screen = useAdminLoad(async () => {
    await refresh(1);
    await api.markFloristPanelNotificationsRead().catch(() => undefined);
  }, [status, delivery, search]);

  const pages = Math.max(1, Math.ceil(total / 10));

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>{t('admin.nav.orders')}</h1>
      <section className="orders-board" aria-label={t('florist.board.aria')}>
        <div className="orders-board__grid">
          {BOARD_COLUMNS.map((col) => {
            const column = boardOrders.filter((o) => normalizeOrderStatus(o.status) === col.status);
            return (
              <div key={col.status} className="orders-board__column card">
                <h2>
                  {col.title} <span className="muted">({column.length})</span>
                </h2>
                {column.slice(0, 8).map((o) => (
                  <article key={o.id} className="orders-board__card">
                    <p>
                      <strong>#{o.id.slice(0, 8)}</strong> · {formatMoney(o.estimatedTotal)}
                    </p>
                    <p className="muted">{o.customerName ?? t('florist.customerFallback')}</p>
                    {nextFloristStatus[normalizeOrderStatus(o.status)] && (
                      <button
                        type="button"
                        onClick={() => {
                          const st = normalizeOrderStatus(o.status);
                          return api
                            .updateOrderStatus(o.id, nextFloristStatus[st]!.next, t('florist.board.updateOnBoard'))
                            .then(() => {
                              refresh();
                            });
                        }}
                      >
                        {nextFloristStatus[normalizeOrderStatus(o.status)]!.label}
                      </button>
                    )}
                  </article>
                ))}
                {column.length === 0 && <p className="muted">{t('florist.board.none')}</p>}
              </div>
            );
          })}
        </div>
      </section>
      <div className="filters row">
        <InlineSelect value={status} onChange={(ev) => setStatus(ev.target.value === '' ? '' : Number(ev.target.value))}>
          <option value="">{pt('Todos os estados')}</option>
          {statusLabels.map((label, i) => (
            <option key={label} value={i}>
              {label}
            </option>
          ))}
        </InlineSelect>
        <InlineSelect value={delivery} onChange={(ev) => setDelivery(ev.target.value === '' ? '' : Number(ev.target.value))}>
          <option value="">{pt('Todas as entregas')}</option>
          {deliveryLabels.map((label, i) => (
            <option key={label} value={i}>
              {label}
            </option>
          ))}
        </InlineSelect>
        <input placeholder="Cliente (nome ou email)" value={search} onChange={(ev) => setSearch(ev.target.value)} />
      </div>

      {listLoading && <p className="muted">{t('common.loading')}</p>}
      {!listLoading && orders.length === 0 && <p className="muted">{t('florist.board.none')}</p>}
      <div className="grid">
        {!listLoading && orders.map((o) => (
          <article key={o.id} className="card">
            <h3>#{o.id.slice(0, 8)}</h3>
            <p>
              {orderStatusLabel(o.status, t)} — {formatMoney(o.estimatedTotal)}
              {o.payment
                ? ` · ${paymentStatusLabel(o.payment.status, t)}`
                : o.paymentCaptured
                  ? ` · ${t('common.paid')}`
                  : ''}
              {o.deliveryFee > 0 && (
                <span className="muted">
                  {' '}
                  ({t('admin.orders.includesDeliveryFee', { fee: formatMoney(o.deliveryFee) })})
                </span>
              )}
            </p>
            <p className="muted">
              {o.customerName ?? 'Cliente'} {o.customerEmail ? `· ${o.customerEmail}` : ''}
            </p>
            <p className="muted">
              {deliveryTypeLabel(o.deliveryType, t)}
              {o.deliveryZoneName ? ` · ${o.deliveryZoneName}` : ''}
              {o.deliveryFee > 0 ? ` · taxa ${formatMoney(o.deliveryFee)}` : ''}
              {o.deliveryAddress ? ` · ${o.deliveryAddress}` : ''}
            </p>
            {o.arrangementPreviewUrl && (
              <img className="arrangement-preview" src={mediaUrl(o.arrangementPreviewUrl)} alt={pt("Pré-visualização do arranjo")} />
            )}
            <button type="button" className="secondary" onClick={() => setOpenArrangement((current) => (current === o.id ? null : o.id))}>
              {openArrangement === o.id ? 'Fechar arranjo 3D' : 'Ver arranjo 3D'}
            </button>
            {openArrangement === o.id && <OrderArrangementPreview order={o} showPreviewImage={false} />}
            {normalizeOrderStatus(o.status) === 0 && (
              <>
                <button type="button" onClick={() => api.confirmOrder(o.id, 'Confirmado no painel').then(() => refresh())}>
                  Confirmar
                </button>
                <textarea
                  placeholder="Motivo da recusa"
                  value={rejectReason[o.id] ?? ''}
                  onChange={(ev) => setRejectReason((prev) => ({ ...prev, [o.id]: ev.target.value }))}
                />
                <button
                  type="button"
                  className="secondary"
                  onClick={() => {
                    const notes = rejectReason[o.id]?.trim();
                    if (!notes) return;
                    api.rejectOrder(o.id, notes).then(() => refresh());
                  }}
                >
                  Recusar
                </button>
                <textarea
                  placeholder="Proposta de alteração"
                  value={proposalMessage[o.id] ?? ''}
                  onChange={(ev) => setProposalMessage((prev) => ({ ...prev, [o.id]: ev.target.value }))}
                />
                <button
                  type="button"
                  onClick={() => {
                    const message = proposalMessage[o.id]?.trim();
                    if (!message) return;
                    const itemsJson = JSON.stringify(
                      o.items.map((i) => ({
                        flowerId: i.flowerId ?? null,
                        name: i.name,
                        quantity: i.quantity,
                        unitPrice: i.unitPrice,
                      })),
                    );
                    api
                      .proposeOrderChanges(o.id, {
                        message,
                        proposedItemChangesJson: itemsJson,
                        revisedTotal: o.estimatedTotal,
                      })
                      .then(() => refresh());
                  }}
                >
                  Propor alterações
                </button>
              </>
            )}
            {nextFloristStatus[normalizeOrderStatus(o.status)] && (
              <button
                type="button"
                onClick={() => {
                  const st = normalizeOrderStatus(o.status);
                  return api
                    .updateOrderStatus(o.id, nextFloristStatus[st]!.next, 'Atualização no painel')
                    .then(() => refresh());
                }}
              >
                {nextFloristStatus[normalizeOrderStatus(o.status)]!.label}
              </button>
            )}
          </article>
        ))}
      </div>
      <div className="row">
        <button type="button" className="fh-btn fh-btn--secondary" disabled={page <= 1} onClick={() => refresh(page - 1)}>
          Anterior
        </button>
        <span className="muted">
          Página {page} / {pages}
        </span>
        <button type="button" className="fh-btn fh-btn--secondary" disabled={page >= pages} onClick={() => refresh(page + 1)}>
          Seguinte
        </button>
      </div>
    </div>
  );
}
