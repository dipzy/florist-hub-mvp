import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { TenantLink } from '../components/TenantLink';
import { Sparkles } from 'lucide-react';
import { api, mediaUrl } from '../api/client';
import type { PortfolioArrangement } from '../api/types';
import { useStoreBranding } from '../context/StoreBrandingContext';
import { Card } from '../components/ui/Card';
import { EmptyState } from '../components/ui/EmptyState';
import { SkeletonGrid } from '../components/ui/Skeleton';
import { formatMoney } from '../lib/locale';

export function PortfolioPage() {
  const [items, setItems] = useState<PortfolioArrangement[] | null>(null);
  const store = useStoreBranding();

  useEffect(() => {
    api.portfolio().then(setItems).catch(() => setItems([]));
  }, []);

  return (
    <section className="page">
      <div className="store-hero">
        <div className="store-hero__overlay">
          <h1>{store.name ?? 'A sua florista online'}</h1>
          <p className="lede">
            Inspire-se com arranjos reais ou monte o seu do zero — flores escolhidas com carinho.
          </p>
          <div className="store-hero__actions">
            <TenantLink to="/montar" className="fh-btn fh-btn--primary fh-btn--lg" style={{ textDecoration: 'none' }}>
              <Sparkles size={18} aria-hidden />
              Montar arranjo
            </TenantLink>
          </div>
        </div>
      </div>

      <header className="page-header">
        <h2>Portfolio</h2>
        <p className="lede muted">Inspire-se e monte o seu arranjo.</p>
      </header>

      {items === null && <SkeletonGrid count={6} />}

      {items?.length === 0 && (
        <EmptyState
          title="Ainda sem arranjos no portfolio"
          description="Volte em breve — a florista está a preparar novas criações."
          action={
            <TenantLink to="/montar" className="fh-btn fh-btn--primary">
              Montar arranjo personalizado
            </TenantLink>
          }
        />
      )}

      <div className="grid">
        {items?.map((p) => (
          <Card key={p.id} elevated className="portfolio-card">
            {p.photos[0] ? (
              <img className="card-hero" src={mediaUrl(p.photos[0].url)} alt={p.title} loading="lazy" />
            ) : (
              <div className="card-hero fh-skeleton fh-skeleton--block" aria-hidden />
            )}
            <h3>{p.slug ? <Link to={`/portfolio/${p.slug}`}>{p.title}</Link> : p.title}</h3>
            {p.description && <p className="muted">{p.description}</p>}
            {p.price != null && <p className="price">{formatMoney(p.price)}</p>}
            <TenantLink className="fh-btn fh-btn--secondary" to={`/montar?inspire=${p.id}`}>
              Usar como base
            </TenantLink>
          </Card>
        ))}
      </div>
    </section>
  );
}
