import { useMemo, useState, type FormEvent } from 'react';
import { usePageText } from '../lib/usePageText';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';

export function FloristInvitePage() {
  const { pt } = usePageText('pages/FloristInvitePage');
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const userId = params.get('userId') ?? '';
  const token = params.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const invalidLink = useMemo(() => !userId || !token, [userId, token]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (password !== confirm) {
      setError(pt("As palavras-passe não coincidem."));
      return;
    }
    if (password.length < 8) {
      setError(pt("A palavra-passe deve ter pelo menos 8 caracteres."));
      return;
    }
    try {
      await api.completeFloristInvite({ userId, token, newPassword: password });
      setDone(true);
      setTimeout(() => navigate('/login?next=/admin/configuracao-inicial'), 2500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível concluir o convite.');
    }
  }

  if (invalidLink) {
    return (
      <section className="page">
        <h1>Convite inválido</h1>
        <p className="error">O link está incompleto ou expirou. Peça um novo convite ao SuperAdmin.</p>
        <Link to="/login">Ir para o login</Link>
      </section>
    );
  }

  if (done) {
    return (
      <section className="page">
        <h1>Palavra-passe definida</h1>
        <p className="success">Já pode iniciar sessão no painel da florista.</p>
      </section>
    );
  }

  return (
    <section className="page">
      <h1>Definir palavra-passe</h1>
      <p className="muted">Conclua o convite para gerir a sua loja no Florist Hub.</p>
      <form className="card form" onSubmit={onSubmit}>
        <label>
          Nova palavra-passe
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
        </label>
        <label>
          Confirmar
          <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required minLength={8} />
        </label>
        {error && <p className="error">{error}</p>}
        <button type="submit">Guardar e continuar</button>
      </form>
    </section>
  );
}
