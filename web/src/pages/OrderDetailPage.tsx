import { usePageText } from '../lib/usePageText';
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { TenantLink } from '../components/TenantLink';
import { formatMoney, formatDate, formatDateTime } from '../lib/locale';
import { api } from '../api/client';
import type { LegalVersions, Order, SalesDocument } from '../api/types';
import { LegalAcceptanceFields } from '../components/LegalAcceptanceFields';
import { buildLegalAcceptance } from '../lib/legalConsent';
import {
  deliveryTypeLabel,
  invoiceKindLabel,
  orderStatusLabel,
  paymentStatusLabel,
  proposalStatusLabel,
} from '../lib/enumLabels';
import { useTranslation } from 'react-i18next';
import { OrderArrangementPreview } from '../configurator/OrderArrangementPreview';
import { OrderStatusBadge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { ErrorState } from '../components/ui/ErrorState';
import { LoadingState } from '../components/ui/LoadingState';
import { InlineSelect } from '../components/ui/InlineSelect';

function canCustomerCancel(status: number) {
  return status === 0 || status === 1;
}

function canPay(order: Order) {
  const open = order.status === 1 || order.status === 2 || order.status === 3;
  const settled = order.payment?.status === 1 || order.payment?.status === 3 || order.paymentCaptured;
  return open && !settled;
}

export function OrderDetailPage() {
  const { t } = useTranslation();
  const { pt } = usePageText('pages/OrderDetailPage');
  const proposalStatusLabels: Record<number, string> = {
    0: proposalStatusLabel(0, t),
    1: proposalStatusLabel(1, t),
    2: t('florist.proposalStatus.clientRejected'),
    3: proposalStatusLabel(3, t),
  };
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<Order | null>(null);
  const [invoices, setInvoices] = useState<SalesDocument[]>([]);
  const [taxId, setTaxId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [legalVersions, setLegalVersions] = useState<LegalVersions | null>(null);
  const [acceptLegal, setAcceptLegal] = useState(false);
  const [couponCode, setCouponCode] = useState('');
  const [stars, setStars] = useState(5);
  const [comment, setComment] = useState('');

  async function reload() {
    if (!id) return;
    const o = await api.getOrder(id);
    setOrder(o);
    setTaxId(o.customerTaxId ?? '');
    const docs = await api.orderInvoices(id).catch(() => []);
    setInvoices(docs);
  }

  useEffect(() => {
    if (!id) return;
    reload().catch((e) => setError(e instanceof Error ? e.message : 'Erro'));
    api.legalVersions().then(setLegalVersions).catch(() => setLegalVersions(null));
  }, [id]);

  async function accept(proposalId: string) {
    if (!order) return;
    await api.acceptProposal(order.id, proposalId, 'Aceite na web');
    await reload();
  }

  async function reject(proposalId: string) {
    if (!order) return;
    await api.rejectProposal(order.id, proposalId, 'Recusada na web');
    await reload();
  }

  async function cancelOrder() {
    if (!order || !window.confirm('Cancelar esta encomenda?')) return;
    await api.cancelOrder(order.id, 'Cancelada pelo cliente na web');
    await reload();
  }

  async function pay() {
    if (!order) return;
    if (!legalVersions || !acceptLegal) {
      setError('Tem de aceitar os Termos e a Política de Privacidade antes de pagar.');
      return;
    }
    const legal = buildLegalAcceptance(legalVersions, acceptLegal);
    setError(null);
    const origin = window.location.origin;
    try {
      const session = await api.startCheckout(
        order.id,
        `${origin}/encomendas/${order.id}/pagamento/sucesso`,
        `${origin}/encomendas/${order.id}/pagamento/cancelado`,
        {
          customerTaxId: taxId,
          ...legal,
        },
      );
      if (session.checkoutUrl) {
        window.location.assign(session.checkoutUrl);
        return;
      }
      setError(session.message ?? 'Não foi possível iniciar o pagamento.');
      await reload();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Não foi possível iniciar o pagamento.');
    }
  }

  if (!order) {
    if (error) return <ErrorState message={error} onRetry={() => reload().catch(() => undefined)} />;
    return <LoadingState message={pt("A carregar a encomenda…")} />;
  }

  return (
    <section className="page">
      <p>
        <TenantLink to="/encomendas">← Voltar às encomendas</TenantLink>
      </p>
      <div className="row spread" style={{ alignItems: 'flex-start', marginBottom: '1rem' }}>
        <h1 style={{ margin: 0 }}>Encomenda #{order.id.slice(0, 8)}</h1>
        <OrderStatusBadge status={order.status} label={orderStatusLabel(order.status, t)} />
      </div>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <p>
        <strong>Total estimado:</strong> {formatMoney(order.estimatedTotal)}
        {order.deliveryFee > 0 && (
          <span className="muted">
            {' '}
            (incl. {formatMoney(order.deliveryFee)} de entrega
            {order.deliveryZoneName ? ` — ${order.deliveryZoneName}` : ''})
          </span>
        )}
        {order.payment && (
          <span className="muted">
            {' '}
            — {t('common.payment')}: {paymentStatusLabel(order.payment.status, t)}
            {order.payment.refundedAmount > 0
              ? ` (reembolsado ${formatMoney(order.payment.refundedAmount)})`
              : ''}
          </span>
        )}
        {!order.payment && order.paymentCaptured && <span className="muted"> — pago</span>}
      </p>
      <p>
        <strong>{pt('Entrega')}:</strong> {deliveryTypeLabel(order.deliveryType, t)}
      </p>
      <OrderArrangementPreview order={order} />
      {order.deliveryAddress && <p>Morada: {order.deliveryAddress}</p>}
      {order.requestedDeliveryAt && (
        <p>
          Data pretendida:{' '}
          {formatDate(order.requestedDeliveryAt, {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
          })}
        </p>
      )}
      {order.trackingAccessToken && (
        <p>
          <TenantLink to={`/acompanhar/${order.trackingAccessToken}`}>
            {t('order.tracking.openLink', { defaultValue: 'Acompanhar encomenda (página pública)' })}
          </TenantLink>
        </p>
      )}
      {order.customerNotes && <p className="muted">Notas: {order.customerNotes}</p>}
      {order.customerTaxId && <p className="muted">NIF na fatura: {order.customerTaxId}</p>}
      {order.inspiredByPortfolioArrangementId && (
        <p className="muted">Inspirada no portfolio: {order.inspiredByPortfolioArrangementId.slice(0, 8)}</p>
      )}

      <h2>Itens</h2>
      <ul className="list compact">
        {order.items.map((i) => (
          <li key={i.id}>
            {i.name} x{i.quantity} — {formatMoney(i.quantity * i.unitPrice)}
          </li>
        ))}
      </ul>

      {(order.changeProposals ?? []).length > 0 && (
        <section>
          <h2>Propostas de alteração</h2>
          {order.changeProposals!.map((p) => (
            <Card key={p.id} className="stack">
              <p>{p.message}</p>
              <p className="muted">
                {proposalStatusLabels[p.status] ?? p.status}
                {p.revisedTotal != null && ` — novo total: ${formatMoney(p.revisedTotal)}`}
              </p>
              {p.status === 0 && (
                <div className="actions row">
                  <button type="button" onClick={() => accept(p.id)}>
                    Aceitar
                  </button>
                  <button type="button" className="secondary" onClick={() => reject(p.id)}>
                    Recusar
                  </button>
                </div>
              )}
            </Card>
          ))}
        </section>
      )}

      {order.discountAmount ? (
        <p>
          Desconto {order.couponCode ? `(${order.couponCode})` : ''}: −{formatMoney(order.discountAmount)}. Total a pagar:{' '}
          {formatMoney(order.estimatedTotal)}
        </p>
      ) : null}
      {canPay(order) && (
        <form
          className="row"
          onSubmit={async (ev) => {
            ev.preventDefault();
            if (!order) return;
            try {
              setOrder(await api.applyCoupon(order.id, couponCode));
              setError(null);
            } catch (err) {
              setError(err instanceof Error ? err.message : 'Cupão recusado.');
            }
          }}
        >
          <input value={couponCode} onChange={(ev) => setCouponCode(ev.target.value)} placeholder={pt("Código do cupão")} />
          <button type="submit">Aplicar cupão</button>
          {order.couponCode && (
            <button
              type="button"
              onClick={async () => {
                setOrder(await api.removeCoupon(order.id));
              }}
            >
              Remover
            </button>
          )}
        </form>
      )}
      {order.status === 4 && !order.review && (
        <form
          className="card form"
          onSubmit={async (ev) => {
            ev.preventDefault();
            await api.reviewOrder(order.id, stars, comment);
            await reload();
          }}
        >
          <h2>Avaliar encomenda</h2>
          <label>
            Estrelas
            <InlineSelect value={stars} onChange={(ev) => setStars(Number(ev.target.value))}>
              {[1, 2, 3, 4, 5].map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </InlineSelect>
          </label>
          <label>
            Comentário
            <textarea value={comment} onChange={(ev) => setComment(ev.target.value)} rows={3} />
          </label>
          <button type="submit">{t('reviews.submit')}</button>
        </form>
      )}
      {order.review && (
        <p>
          {t('reviews.yourReview')} {order.review.stars} {t('reviews.stars')}. {order.review.comment}
          {order.review.floristReply ? ` ${t('reviews.reply')}: ${order.review.floristReply}` : ''}
        </p>
      )}

      <h2>Linha do tempo</h2>
      <ol className="timeline">
        {(order.statusHistory ?? []).map((h) => (
          <li key={h.id}>
            <time>{formatDateTime(h.createdAt)}</time>
            <p>
              {orderStatusLabel(h.fromStatus, t)} → {orderStatusLabel(h.toStatus, t)}
            </p>
            <p className="muted">{h.notes ?? '—'}</p>
          </li>
        ))}
      </ol>

      {invoices.length > 0 && (
        <section>
          <h2>Faturas</h2>
          <ul className="list compact">
            {invoices.map((document) => (
              <li key={document.id}>
                {invoiceKindLabel(document.kind, t)}
                {document.documentNumber ? ` ${document.documentNumber}` : ''} — {formatMoney(document.grossAmount)}
                {document.status === 1 && (
                  <button
                    type="button"
                    className="secondary"
                    onClick={() => api.downloadInvoicePdf(document.id, `${document.documentNumber || 'fatura'}.pdf`)}
                  >
                    Descarregar PDF
                  </button>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="actions row">
        {canCustomerCancel(order.status) && (
          <button type="button" className="secondary" onClick={cancelOrder}>
            Cancelar encomenda
          </button>
        )}
        {canPay(order) && (
          <div className="stack">
            <LegalAcceptanceFields versions={legalVersions} checked={acceptLegal} onCheckedChange={setAcceptLegal} />
            <label>
              NIF na fatura (opcional)
              <input
                value={taxId}
                inputMode="numeric"
                onChange={(ev) => setTaxId(ev.target.value)}
                placeholder="Consumidor final se ficar vazio"
              />
            </label>
            <button type="button" onClick={pay}>
              Pagar
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
