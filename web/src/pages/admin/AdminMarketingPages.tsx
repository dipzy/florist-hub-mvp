import { useState, type FormEvent } from 'react';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api } from '../../api/client';
import type { Campaign, Coupon, FloristReview, Flower } from '../../api/types';
import { formatMoney } from '../../lib/locale';
import { InlineSelect } from '../../components/ui/InlineSelect';

export function AdminCouponsPage() {
  const [items, setItems] = useState<Coupon[]>([]);
  const [flowers, setFlowers] = useState<Flower[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [discountType, setDiscountType] = useState(0);
  const [discountValue, setDiscountValue] = useState(10);
  const [minimum, setMinimum] = useState('');
  const [maxTotal, setMaxTotal] = useState('');
  const [maxCustomer, setMaxCustomer] = useState('');
  const [validUntil, setValidUntil] = useState('');
  const [category, setCategory] = useState('');
  const [flowerId, setFlowerId] = useState('');

  async function refresh() {
    setItems(await api.coupons());
  }

  const screen = useAdminLoad(async () => {
    await refresh();
    setFlowers(await api.flowers());
  }, []);

  async function save(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    const targets = [
      ...(category.trim() ? [{ category: category.trim(), flowerId: null }] : []),
      ...(flowerId ? [{ flowerId, category: null }] : []),
    ];
    try {
      await api.saveCoupon({
        code,
        discountType,
        discountValue,
        validUntil: validUntil ? new Date(validUntil).toISOString() : null,
        maxRedemptions: maxTotal ? Number(maxTotal) : null,
        maxRedemptionsPerCustomer: maxCustomer ? Number(maxCustomer) : null,
        minimumOrderAmount: minimum ? Number(minimum) : null,
        isActive: true,
        targets,
      });
      setCode('');
      setMsg('Cupão guardado. A validação é feita no servidor.');
      await refresh();
    } catch (err) {
      setMsg(err instanceof Error ? err.message : 'Não foi possível guardar o cupão.');
    }
  }

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Cupões</h1>
      <form className="card form admin-form" onSubmit={save}>
        <label>
          Código
          <input value={code} onChange={(ev) => setCode(ev.target.value)} required minLength={3} />
        </label>
        <label>
          Tipo
          <InlineSelect value={discountType} onChange={(ev) => setDiscountType(Number(ev.target.value))}>
            <option value={0}>Percentagem</option>
            <option value={1}>Valor fixo (€)</option>
          </InlineSelect>
        </label>
        <label>
          Desconto
          <input type="number" step="0.01" value={discountValue} onChange={(ev) => setDiscountValue(Number(ev.target.value))} />
        </label>
        <label>
          Valor mínimo da encomenda (€)
          <input value={minimum} onChange={(ev) => setMinimum(ev.target.value)} />
        </label>
        <label>
          Utilizações máximas
          <input value={maxTotal} onChange={(ev) => setMaxTotal(ev.target.value)} />
        </label>
        <label>
          Máximo por cliente
          <input value={maxCustomer} onChange={(ev) => setMaxCustomer(ev.target.value)} />
        </label>
        <label>
          Válido até
          <input type="date" value={validUntil} onChange={(ev) => setValidUntil(ev.target.value)} />
        </label>
        <label>
          Categoria abrangida
          <input value={category} onChange={(ev) => setCategory(ev.target.value)} placeholder="rosas" />
        </label>
        <label>
          Flor abrangida
          <InlineSelect value={flowerId} onChange={(ev) => setFlowerId(ev.target.value)}>
            <option value="">Todas, se não indicar categoria</option>
            {flowers.map((flower) => (
              <option key={flower.id} value={flower.id}>
                {flower.name}
              </option>
            ))}
          </InlineSelect>
        </label>
        <button type="submit">Criar cupão</button>
      </form>
      {msg && <p>{msg}</p>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Código</th>
            <th>Desconto</th>
            <th>Usos</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((coupon) => (
            <tr key={coupon.id}>
              <td>{coupon.code}</td>
              <td>{coupon.discountType === 0 ? `${coupon.discountValue}%` : formatMoney(coupon.discountValue)}</td>
              <td>{coupon.redemptionCount}</td>
              <td>
                <button type="button" onClick={() => api.deleteCoupon(coupon.id).then(refresh)}>
                  Desativar
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function AdminCampaignsPage() {
  const [items, setItems] = useState<Campaign[]>([]);
  const [flowers, setFlowers] = useState<Flower[]>([]);
  const [name, setName] = useState('Dia da Mãe');
  const [description, setDescription] = useState('');
  const [startsOn, setStartsOn] = useState('');
  const [endsOn, setEndsOn] = useState('');
  const [flowerId, setFlowerId] = useState('');
  const [specialPrice, setSpecialPrice] = useState('');
  const [msg, setMsg] = useState<string | null>(null);

  async function refresh() {
    setItems(await api.campaigns());
  }

  const screen = useAdminLoad(async () => {
    await refresh();
    setFlowers(await api.flowers());
  }, []);

  async function save(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    try {
      await api.saveCampaign({
        name,
        description,
        startsOn,
        endsOn,
        highlightOnHome: true,
        isActive: true,
        flowers: flowerId
          ? [{ flowerId, specialPrice: specialPrice ? Number(specialPrice) : null, sortOrder: 0 }]
          : [],
      });
      setMsg('Campanha guardada.');
      await refresh();
    } catch (err) {
      setMsg(err instanceof Error ? err.message : 'Não foi possível guardar a campanha.');
    }
  }

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Datas especiais</h1>
      <form className="card form admin-form" onSubmit={save}>
        <label>
          Nome
          <input value={name} onChange={(ev) => setName(ev.target.value)} required />
        </label>
        <label>
          Descrição
          <textarea value={description} onChange={(ev) => setDescription(ev.target.value)} rows={3} />
        </label>
        <label>
          Início
          <input type="date" value={startsOn} onChange={(ev) => setStartsOn(ev.target.value)} required />
        </label>
        <label>
          Fim
          <input type="date" value={endsOn} onChange={(ev) => setEndsOn(ev.target.value)} required />
        </label>
        <label>
          Flor em destaque
          <InlineSelect value={flowerId} onChange={(ev) => setFlowerId(ev.target.value)}>
            <option value="">Nenhuma</option>
            {flowers.map((flower) => (
              <option key={flower.id} value={flower.id}>
                {flower.name}
              </option>
            ))}
          </InlineSelect>
        </label>
        <label>
          Preço especial (€)
          <input value={specialPrice} onChange={(ev) => setSpecialPrice(ev.target.value)} />
        </label>
        <button type="submit">Criar campanha</button>
      </form>
      {msg && <p>{msg}</p>}
      <ul>
        {items.map((campaign) => (
          <li key={campaign.id}>
            {campaign.name} ({campaign.startsOn} – {campaign.endsOn}){' '}
            {campaign.highlightOnHome ? '· destaque na loja' : ''}{' '}
            <button type="button" onClick={() => api.deleteCampaign(campaign.id).then(refresh)}>
              Apagar
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function AdminReviewsPage() {
  const [items, setItems] = useState<FloristReview[]>([]);
  const [reply, setReply] = useState<Record<string, string>>({});

  async function refresh() {
    setItems(await api.floristReviews());
  }

  const screen = useAdminLoad(() => refresh(), []);

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Avaliações</h1>
      {items.length === 0 && <p className="muted">Ainda não há avaliações.</p>}
      {items.map((review) => (
        <article key={review.id} className="card">
          <p>
            {'★'.repeat(review.stars)} {review.customerName ?? 'Cliente'}
            {review.isHidden ? ' · oculta' : ''}
            {!review.isApproved && !review.isHidden ? ' · por aprovar' : ''}
            {review.isApproved && !review.isHidden ? ' · na loja' : ''}
          </p>
          {review.comment && <p>{review.comment}</p>}
          {review.floristReply && <p className="muted">Resposta: {review.floristReply}</p>}
          <label>
            Responder
            <input value={reply[review.id] ?? ''} onChange={(ev) => setReply({ ...reply, [review.id]: ev.target.value })} />
          </label>
          {!review.isApproved && !review.isHidden && (
            <button type="button" onClick={() => api.approveReview(review.id).then(refresh)}>
              Aprovar na loja
            </button>
          )}
          <button
            type="button"
            onClick={() => api.replyReview(review.id, reply[review.id] ?? '').then(refresh)}
          >
            Publicar resposta
          </button>
          <button
            type="button"
            onClick={() => api.hideReview(review.id, !review.isHidden, review.isHidden ? '' : 'Conteúdo abusivo').then(refresh)}
          >
            {review.isHidden ? 'Mostrar outra vez' : 'Ocultar'}
          </button>
        </article>
      ))}
    </div>
  );
}
