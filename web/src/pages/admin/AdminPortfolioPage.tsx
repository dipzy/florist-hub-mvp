import { useState, type FormEvent } from 'react';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api, mediaUrl } from '../../api/client';
import { FileField } from '../../components/ui/FileField';
import type { PortfolioArrangement } from '../../api/types';

export function AdminPortfolioPage() {
  const [items, setItems] = useState<PortfolioArrangement[]>([]);
  const [editing, setEditing] = useState<PortfolioArrangement | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [photoUrls, setPhotoUrls] = useState<string[]>([]);
  const [msg, setMsg] = useState<string | null>(null);

  async function refresh() {
    setItems(await api.portfolioManage());
  }

  const screen = useAdminLoad(() => refresh(), []);

  function startEdit(p: PortfolioArrangement) {
    setEditing(p);
    setTitle(p.title);
    setDescription(p.description ?? '');
    setPhotoUrls(p.photos.map((x) => x.url));
  }

  function resetForm() {
    setEditing(null);
    setTitle('');
    setDescription('');
    setPhotoUrls([]);
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    const body = {
      title,
      description,
      price: editing?.price,
      tags: editing?.tags,
      status: editing?.status ?? 0,
      photos: photoUrls.map((url, sortOrder) => ({ url, sortOrder })),
      items: editing?.items ?? [],
    };
    try {
      if (editing) await api.updatePortfolio(editing.id, body);
      else await api.createPortfolio(body);
      resetForm();
      setMsg('Portfolio guardado.');
      await refresh();
    } catch {
      setMsg('Erro ao guardar arranjo.');
    }
  }

  async function toggleStatus(p: PortfolioArrangement) {
    const next = p.status === 0 ? 1 : 0;
    await api.setPortfolioStatus(p.id, next);
    await refresh();
  }

  async function move(p: PortfolioArrangement, direction: -1 | 1) {
    const sorted = [...items].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
    const index = sorted.findIndex((x) => x.id === p.id);
    const swap = sorted[index + direction];
    if (!swap) return;
    const payload = sorted.map((x, i) => {
      if (x.id === p.id) return { id: x.id, sortOrder: index + direction };
      if (x.id === swap.id) return { id: x.id, sortOrder: index };
      return { id: x.id, sortOrder: i };
    });
    await api.sortPortfolio(payload);
    await refresh();
  }

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>Portfolio</h1>
      <ul className="list">
        {[...items]
          .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
          .map((p) => (
            <li key={p.id} className="card row spread">
              <div>
                {p.photos[0] && <img src={mediaUrl(p.photos[0].url)} alt="" width={64} height={64} />}
                <strong>{p.title}</strong> — {p.status === 0 ? 'Publicado' : 'Oculto'}
              </div>
              <div className="row">
                <button type="button" className="secondary" onClick={() => move(p, -1)}>
                  ↑
                </button>
                <button type="button" className="secondary" onClick={() => move(p, 1)}>
                  ↓
                </button>
                <button type="button" onClick={() => toggleStatus(p)}>
                  {p.status === 0 ? 'Despublicar' : 'Publicar'}
                </button>
                <button type="button" onClick={() => startEdit(p)}>
                  Editar
                </button>
              </div>
            </li>
          ))}
      </ul>

      <form className="card form admin-form" onSubmit={save}>
        <h2>{editing ? 'Editar arranjo' : 'Novo arranjo'}</h2>
        <label>
          Título
          <input value={title} onChange={(ev) => setTitle(ev.target.value)} required />
        </label>
        <label>
          Descrição
          <textarea value={description} onChange={(ev) => setDescription(ev.target.value)} rows={2} />
        </label>
        <FileField
          label="Fotos"
          multiple
          onFiles={async (files) => {
            const uploaded: string[] = [];
            for (const file of files) {
              uploaded.push((await api.uploadPortfolioPhoto(file)).url);
            }
            setPhotoUrls((prev) => [...prev, ...uploaded]);
          }}
        />
        <div className="thumbs">
          {photoUrls.map((url) => (
            <img key={url} src={mediaUrl(url)} alt="" width={72} height={72} />
          ))}
        </div>
        <div className="row">
          <button type="submit">Guardar</button>
          {editing && (
            <button type="button" className="secondary" onClick={resetForm}>
              Cancelar
            </button>
          )}
        </div>
        {msg && <p className="muted">{msg}</p>}
      </form>
    </div>
  );
}
