import { useEffect, useState } from 'react';
import { usePageText } from '../lib/usePageText';
import { useParams } from 'react-router-dom';
import { TenantLink } from '../components/TenantLink';
import { Sparkles } from 'lucide-react';
import { api, mediaUrl } from '../api/client';
import type { Flower, PortfolioArrangement, PublicCampaign, PublicSeasonalFlower, PublicStorefront } from '../api/types';
import { Card } from '../components/ui/Card';
import { EmptyState } from '../components/ui/EmptyState';
import { LoadingState } from '../components/ui/LoadingState';
import { useStoreBranding } from '../context/StoreBrandingContext';
import { formatMoney } from '../lib/locale';
import { ensurePublicTenantResolved } from '../lib/publicTenant';
import { useTranslation } from 'react-i18next';

function usePageMeta(title: string, description: string) {
  useEffect(() => {
    const previous = document.title;
    document.title = title;
    let meta = document.querySelector('meta[name="description"]');
    if (!meta) {
      meta = document.createElement('meta');
      meta.setAttribute('name', 'description');
      document.head.appendChild(meta);
    }
    const old = meta.getAttribute('content');
    meta.setAttribute('content', description);
    return () => {
      document.title = previous;
      if (old) meta?.setAttribute('content', old);
    };
  }, [title, description]);
}

function Stars({ value }: { value: number }) {
  return <span aria-label={`${value} em 5 estrelas`}>{'★'.repeat(value)}{'☆'.repeat(Math.max(0, 5 - value))}</span>;
}

function asPortfolio(raw: unknown): PortfolioArrangement[] | null {
  if (Array.isArray(raw)) return raw as PortfolioArrangement[];
  if (raw && typeof raw === 'object' && Array.isArray((raw as { items?: unknown }).items)) {
    return (raw as { items: PortfolioArrangement[] }).items;
  }
  return null;
}

function CardImage({ src, alt }: { src?: string | null; alt: string }) {
  if (!src) return <div className="card-hero card-hero--placeholder" role="img" aria-label={alt} />;
  return <img className="card-hero" src={mediaUrl(src)} alt={alt} />;
}

export function StoreHomePage() {
  const { pt } = usePageText('pages/StorePublicPages');
  const branding = useStoreBranding();
  const [data, setData] = useState<PublicStorefront | null>(null);
  const [featured, setFeatured] = useState<PortfolioArrangement[] | null>(null);
  const [flowers, setFlowers] = useState<Flower[]>([]);
  const [ready, setReady] = useState(false);
  useEffect(() => {
    let cancel = false;
    setReady(false);
    (async () => {
      await ensurePublicTenantResolved();
      if (cancel) return;
      const [home, portfolioRaw, flowerList] = await Promise.all([
        api.storeHome().catch(() => null),
        api.portfolio().then(asPortfolio).catch(() => null),
        api.flowers().catch(() => [] as Flower[]),
      ]);
      if (cancel) return;
      setData(home);
      setFeatured(portfolioRaw ? portfolioRaw.slice(0, 6) : null);
      setFlowers(flowerList.filter((flower) => flower.imageUrl).slice(0, 6));
      setReady(true);
    })();
    return () => {
      cancel = true;
    };
  }, [branding.loading, branding.name]);
  const storeName = data?.name || branding.name;
  usePageMeta(
    storeName ? `${storeName} — florista` : 'Loja',
    data?.description || 'Encomende flores e arranjos nesta florista.',
  );

  const [descExpanded, setDescExpanded] = useState(false);
  const longDescription = Boolean(data?.description && data.description.length > 220);
  const heroDescription =
    longDescription && !descExpanded
      ? `${data!.description!.slice(0, 217).trim()}…`
      : data?.description;

  const portfolioPhoto = featured?.find((item) => item.photos[0]?.url)?.photos[0]?.url;
  const logoLike = data?.heroImageUrl?.toLowerCase().includes('logo') ?? false;
  const heroPhoto =
    portfolioPhoto ??
    (!logoLike ? data?.heroImageUrl : undefined) ??
    (featured && featured.length === 0 ? flowers.find((flower) => flower.imageUrl)?.imageUrl : undefined);
  const heroStyle = heroPhoto ? { backgroundImage: `url(${mediaUrl(heroPhoto)})` } : undefined;
  const showPortfolio = (featured?.length ?? 0) > 0;
  const showFlowers = ready && featured?.length === 0 && flowers.length > 0;

  return (
    <section className="page">
      <div className={`store-hero${heroPhoto ? '' : ' store-hero--pending'}`} style={heroStyle}>
        <div className="store-hero__overlay">
          <h1>{storeName || 'A sua florista online'}</h1>
          {heroDescription && <p className="lede">{heroDescription}</p>}
          {longDescription && (
            <button type="button" className="button-link store-hero__read-more" onClick={() => setDescExpanded((v) => !v)}>
              {descExpanded ? 'Mostrar menos' : 'Ler mais'}
            </button>
          )}
          {data && data.reviewCount > 0 && (
            <p className="muted">
              <Stars value={Math.round(data.averageRating)} /> {data.averageRating.toFixed(1)} · {data.reviewCount}{' '}
              {data.reviewCount === 1 ? 'avaliação' : 'avaliações'}
            </p>
          )}
          <div className="store-hero__actions">
            <TenantLink to="/montar" className="fh-btn fh-btn--primary fh-btn--lg" style={{ textDecoration: 'none' }}>
              <Sparkles size={18} aria-hidden />
              Montar arranjo
            </TenantLink>
          </div>
        </div>
      </div>

      <header className="page-header">
        <h2>Arranjos em destaque</h2>
        <p className="lede muted">Inspire-se e personalize numa nova encomenda.</p>
      </header>

      {!ready ? (
        <div className="grid" aria-busy="true">
          {Array.from({ length: 3 }, (_, index) => (
            <Card key={index} elevated>
              <div className="card-hero card-hero--placeholder" />
            </Card>
          ))}
        </div>
      ) : showPortfolio ? (
        <div className="grid">
          {featured!.map((item) => (
            <Card key={item.id} elevated>
              <CardImage src={item.photos[0]?.url} alt={item.title} />
              <h3>{item.title}</h3>
              {item.description && <p className="muted">{item.description}</p>}
              <p>
                <TenantLink to={`/montar?inspire=${item.id}`}>Usar como inspiração</TenantLink>
                {' · '}
                <TenantLink to={`/portfolio/${item.slug}`}>Detalhe</TenantLink>
              </p>
            </Card>
          ))}
        </div>
      ) : showFlowers ? (
        <div className="grid">
          {flowers.map((flower) => (
            <Card key={flower.id} elevated>
              <CardImage src={flower.imageUrl} alt={flower.name} />
              <h3>{flower.name}</h3>
              <p className="price">{formatMoney(flower.unitPrice)}</p>
              <p>
                <TenantLink to="/montar">Montar arranjo</TenantLink>
              </p>
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState
          title={pt("Em breve novos arranjos")}
          description="Explore o portfolio ou monte o seu arranjo com o catálogo da loja."
        />
      )}

      <p className="muted" style={{ marginTop: '1.25rem' }}>
        <TenantLink to="/portfolio">Ver portfolio completo</TenantLink>
      </p>
    </section>
  );
}

export function SeasonalFlowersPage() {
  const { pt } = usePageText('pages/StorePublicPages');
  const [flowers, setFlowers] = useState<PublicSeasonalFlower[] | null>(null);
  const [missing, setMissing] = useState(false);
  useEffect(() => {
    api.seasonalFlowers()
      .then(setFlowers)
      .catch(() => setMissing(true));
  }, []);
  usePageMeta('Flores da época', 'Flores de época disponíveis nesta loja.');
  if (missing) return <EmptyState title="Loja não encontrada" description="Abra a página a partir da florista." />;
  if (!flowers) return <LoadingState message={pt("A carregar flores da época…")} />;
  return (
    <section className="page">
      <header className="page-header">
        <h1>Flores da época</h1>
      </header>
      {flowers.length === 0 ? (
        <EmptyState title="Sem flores de época este mês" description={pt("O catálogo completo continua disponível ao montar um arranjo.")} />
      ) : (
        <div className="grid">
          {flowers.map((flower) => (
            <Card key={flower.id}>
              {flower.imageUrl && <img className="card-hero" src={mediaUrl(flower.imageUrl)} alt={flower.name} />}
              <h2>{flower.name}{flower.isOutOfStock ? ' — esgotada' : ''}</h2>
              <p className="price">
                {formatMoney(flower.specialPrice != null ? flower.specialPrice : flower.unitPrice)}
              </p>
            </Card>
          ))}
        </div>
      )}
      <p>
        <TenantLink to="/montar">Encomendar</TenantLink>
      </p>
    </section>
  );
}

export function CampaignPage() {
  const { pt } = usePageText('pages/StorePublicPages');
  const { t } = useTranslation();
  const { slug = '' } = useParams();
  const [campaign, setCampaign] = useState<PublicCampaign | null>(null);
  const [missing, setMissing] = useState(false);
  useEffect(() => {
    api.storeCampaign(slug).then(setCampaign).catch(() => setMissing(true));
  }, [slug]);
  usePageMeta(campaign ? `${campaign.name} — campanha` : 'Campanha', campaign?.description || 'Campanha da florista.');
  if (missing) return <EmptyState title="Campanha indisponível" description="Esta campanha não está ativa." />;
  if (!campaign) return <LoadingState message={pt("A carregar campanha…")} />;
  return (
    <section className="page">
      <header className="page-header">
        <h1>{campaign.name}</h1>
        {campaign.description && <p className="lede">{campaign.description}</p>}
      </header>
      <div className="grid">
        {campaign.flowers.map((flower) => (
          <Card key={flower.flowerId} padding="sm">
            <h3 style={{ margin: 0 }}>{flower.name}</h3>
            <p className="price" style={{ margin: '0.5rem 0 0' }}>
              {flower.isOutOfStock ? t('common.outOfStock') : formatMoney(flower.specialPrice ?? flower.catalogPrice)}
            </p>
          </Card>
        ))}
      </div>
      <p className="actions row">
        <TenantLink to="/montar" className="fh-btn fh-btn--primary">
          Montar arranjo
        </TenantLink>
      </p>
    </section>
  );
}

export function PortfolioDetailPage() {
  const { pt } = usePageText('pages/StorePublicPages');
  const { slug = '' } = useParams();
  const [item, setItem] = useState<PortfolioArrangement | null>(null);
  const [missing, setMissing] = useState(false);
  useEffect(() => {
    api.portfolioBySlug(slug).then(setItem).catch(() => setMissing(true));
  }, [slug]);
  usePageMeta(item ? `${item.title} — portfolio` : 'Portfolio', item?.description || 'Arranjo do portfolio.');
  if (missing) return <EmptyState title="Arranjo não encontrado" description="Este endereço não corresponde a um arranjo publicado." />;
  if (!item) return <LoadingState message={pt("A carregar arranjo…")} />;
  return (
    <section className="page">
      <header className="page-header">
        <h1>{item.title}</h1>
      </header>
      {item.photos[0] && <img className="card-hero" src={mediaUrl(item.photos[0].url)} alt={item.title} />}
      {item.description && <p>{item.description}</p>}
      {item.price != null && <p className="price">{formatMoney(item.price)}</p>}
      <TenantLink className="fh-btn fh-btn--secondary" to={`/montar?inspire=${item.id}`}>
        Usar como base
      </TenantLink>
    </section>
  );
}
