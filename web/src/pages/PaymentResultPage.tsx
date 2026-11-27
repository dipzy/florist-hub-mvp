import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Order } from '../api/types';
import { PAYMENT_STATUS_LABELS } from '../api/types';
import { formatMoney } from '../lib/locale';

export function PaymentResultPage({ outcome }: { outcome: 'sucesso' | 'cancelado' }) {
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    api.getOrder(id).then(setOrder).catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar a encomenda.'));
  }, [id]);

  const paid = order?.payment?.status === 1 || order?.paymentCaptured;
  const title = outcome === 'sucesso' ? (paid ? 'Pagamento recebido' : 'Pagamento em confirmação') : 'Pagamento cancelado';
  const text =
    outcome === 'sucesso'
      ? paid
        ? 'O pagamento foi registado. A florista pode avançar com a encomenda.'
        : 'Se pagou com Multibanco, a confirmação pode demorar até o banco aceitar a referência. Esta página actualiza o estado quando o Stripe confirmar.'
      : 'O pagamento não foi concluído. Pode voltar à encomenda e tentar de novo.';

  return (
    <section className="page">
      <h1>{title}</h1>
      <p>{text}</p>
      {order?.payment && (
        <p className="muted">
          Estado: {PAYMENT_STATUS_LABELS[order.payment.status] ?? order.payment.status}
          {order.payment.refundedAmount > 0 ? ` · reembolsado ${formatMoney(order.payment.refundedAmount)}` : ''}
        </p>
      )}
      {error && <p className="error">{error}</p>}
      {id && <Link to={`/encomendas/${id}`}>Voltar à encomenda</Link>}
    </section>
  );
}
