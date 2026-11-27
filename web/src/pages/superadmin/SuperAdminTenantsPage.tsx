import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { Building2 } from 'lucide-react';
import { api } from '../../api/client';
import type { CreatePlatformTenantResponse, PlatformTenantListItem } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { EmptyState } from '../../components/ui/EmptyState';
import { Input } from '../../components/ui/Input';
import { Table } from '../../components/ui/Table';
import { createTenantSuccessMessage } from '../../lib/createTenantSuccessMessage';

export function SuperAdminTenantsPage() {
  const [items, setItems] = useState<PlatformTenantListItem[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatePlatformTenantResponse | null>(null);
  const [devInviteLink, setDevInviteLink] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    name: '',
    slug: '',
    floristEmail: '',
    floristFullName: '',
    customDomain: '',
  });

  async function load() {
    setItems(await api.platformTenants());
    setLoaded(true);
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar lojas.'));
  }, []);

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreated(null);
    setDevInviteLink(null);
    try {
      const res = await api.createPlatformTenant({
        name: form.name,
        slug: form.slug,
        floristEmail: form.floristEmail,
        floristFullName: form.floristFullName,
        customDomain: form.customDomain || undefined,
      });
      setCreated(res);
      if (res.devInviteLink) setDevInviteLink(res.devInviteLink);
      setShowForm(false);
      setForm({ name: '', slug: '', floristEmail: '', floristFullName: '', customDomain: '' });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível criar a loja.');
    }
  }

  return (
    <div className="page">
      <header className="page-header row spread">
        <div>
          <h1>Lojas</h1>
          <p className="lede muted">Floristas na plataforma.</p>
        </div>
        <Button type="button" variant={showForm ? 'secondary' : 'primary'} onClick={() => setShowForm((v) => !v)}>
          {showForm ? 'Cancelar' : 'Nova loja'}
        </Button>
      </header>

      {created && (
        <Card className="notice" padding="md">
          <p>{createTenantSuccessMessage(created.tenant.name, created.floristInviteSent)}</p>
          {devInviteLink && (
            <p className="muted">
              Modo dev (<code>Email__Provider=Log</code>): link de convite (uma vez):{' '}
              <a href={devInviteLink}>{devInviteLink}</a>
            </p>
          )}
        </Card>
      )}

      {showForm && (
        <Card padding="lg" elevated>
          <h2 style={{ marginTop: 0 }}>Nova loja</h2>
          <form className="form admin-form" onSubmit={onCreate}>
            <Input
              label="Nome da loja"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
            <Input
              label="Slug (subdomínio)"
              value={form.slug}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
              required
            />
            <Input
              label="Domínio próprio (opcional)"
              value={form.customDomain}
              onChange={(e) => setForm({ ...form, customDomain: e.target.value })}
            />
            <Input
              label="Correio da florista"
              type="email"
              value={form.floristEmail}
              onChange={(e) => setForm({ ...form, floristEmail: e.target.value })}
              required
              autoComplete="email"
            />
            <Input
              label="Nome da florista"
              value={form.floristFullName}
              onChange={(e) => setForm({ ...form, floristFullName: e.target.value })}
              required
            />
            <Button type="submit" variant="primary">
              Criar loja
            </Button>
          </form>
        </Card>
      )}

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {loaded && items.length === 0 && !showForm ? (
        <EmptyState
          title="Ainda não há lojas"
          description="Crie a primeira florista para começar a receber encomendas na plataforma."
          icon={<Building2 size={40} />}
          action={
            <Button type="button" variant="primary" onClick={() => setShowForm(true)}>
              Nova loja
            </Button>
          }
        />
      ) : items.length > 0 ? (
        <Table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>Slug</th>
              <th>Estado</th>
              <th>Encomendas</th>
              <th>Criada</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((t) => (
              <tr key={t.id}>
                <td>{t.name}</td>
                <td>{t.slug}</td>
                <td>{t.isActive ? 'Ativa' : 'Suspensa'}</td>
                <td>{t.orderCount}</td>
                <td>{new Date(t.createdAt).toLocaleDateString('pt-PT')}</td>
                <td>
                  <Link to={`/superadmin/lojas/${t.id}`}>Detalhe</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      ) : null}
    </div>
  );
}
