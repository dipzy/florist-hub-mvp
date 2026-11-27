import { useState } from 'react';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api } from '../../api/client';
import type { FloristCustomer } from '../../api/types';
import { formatMoney } from '../../lib/locale';

export function AdminCustomersPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [items, setItems] = useState<FloristCustomer[]>([]);

  async function refresh(p = page) {
    const res = await api.floristCustomers({ search: search || undefined, page: p, pageSize: 20 });
    setItems(res.items);
    setTotal(res.total);
    setPage(res.page);
  }

  const screen = useAdminLoad(() => refresh(1), [search]);

  const pages = Math.max(1, Math.ceil(total / 20));

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Clientes</h1>
      <input placeholder="Pesquisar…" value={search} onChange={(ev) => setSearch(ev.target.value)} />
      <table className="admin-table">
        <thead>
          <tr>
            <th>Nome</th>
            <th>Email</th>
            <th>Encomendas</th>
            <th>Total (€)</th>
          </tr>
        </thead>
        <tbody>
          {items.map((c) => (
            <tr key={c.customerId}>
              <td>{c.fullName ?? '—'}</td>
              <td>{c.email ?? '—'}</td>
              <td>{c.orderCount}</td>
              <td>{formatMoney(c.totalSpent)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="row">
        <button type="button" disabled={page <= 1} onClick={() => refresh(page - 1)}>
          Anterior
        </button>
        <span className="muted">
          Página {page} / {pages}
        </span>
        <button type="button" disabled={page >= pages} onClick={() => refresh(page + 1)}>
          Seguinte
        </button>
      </div>
    </div>
  );
}
