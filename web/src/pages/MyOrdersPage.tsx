import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { TenantLink } from '../components/TenantLink';
import { Package } from 'lucide-react';
import { api } from '../api/client';
import type { Order } from '../api/types';
import { Card } from '../components/ui/Card';
import { orderStatusLabel } from '../lib/enumLabels';
import { formatMoney } from '../lib/locale';
import { usePageText } from '../lib/usePageText';
import { EmptyState } from '../components/ui/EmptyState';
import { OrderStatusBadge } from '../components/ui/Badge';
import { pluralPt } from '../lib/pluralPt';
import { SkeletonGrid } from '../components/ui/Skeleton';

export function MyOrdersPage() {
  const { t, pt } = usePageText('pages/MyOrdersPage');
  const [orders, setOrders] = useState<Order[] | null>(null);

  useEffect(() => {
    api.myOrders().then(setOrders).catch(() => setOrders([]));
  }, []);

  return (
    <section className="page">
      <header className="page-header">
        <h1>{pt('As minhas encomendas')}</h1>
        <p className="lede muted">{pt('Estado e detalhe dos seus pedidos.')}</p>
      </header>

      {orders === null && <SkeletonGrid count={3} />}

      {orders?.length === 0 && (
        <EmptyState
          icon={<Package size={40} />}
          title={pt('Ainda não tem encomendas')}
          description={pt('Monte um arranjo e submeta o primeiro pedido à florista.')}
          action={
            <TenantLink to="/montar" className="fh-btn fh-btn--primary">
              {pt('Ir para montar arranjo')}
            </TenantLink>
          }
        />
      )}

      <div className="grid">
        {orders?.map((o) => (
          <Card key={o.id} elevated>
            <div className="row spread">
              <h2 style={{ margin: 0 }}>
                <Link to={`/encomendas/${o.id}`}>#{o.id.slice(0, 8)}</Link>
              </h2>
              <OrderStatusBadge
                status={o.status}
                label={orderStatusLabel(o.status, t)}
              />
            </div>
            <p className="price">
              {pt('Total')}: {formatMoney(o.estimatedTotal)}
            </p>
            <ul className="list compact">
              {o.items.slice(0, 4).map((i) => (
                <li key={i.id}>
                  {i.name} ×{i.quantity}
                </li>
              ))}
              {o.items.length > 4 && (
                <li className="muted">
                  +{o.items.length - 4}{' '}
                  {pluralPt(o.items.length - 4, 'item', 'itens')}
                </li>
              )}
            </ul>
            <Link className="fh-btn fh-btn--secondary" to={`/encomendas/${o.id}`}>
              {pt('Ver detalhe e linha do tempo')}
            </Link>
          </Card>
        ))}
      </div>
    </section>
  );
}
