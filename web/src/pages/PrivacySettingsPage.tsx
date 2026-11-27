import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, saveSession } from '../api/client';
import { useAuth } from '../context/AuthContext';

export function PrivacySettingsPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function exportData() {
    setBusy(true);
    setMessage(null);
    try {
      const data = await api.exportMyData();
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `floristhub-dados-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
      URL.revokeObjectURL(url);
      setMessage('Exportação concluída.');
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Falha na exportação.');
    } finally {
      setBusy(false);
    }
  }

  async function anonymize() {
    if (!window.confirm('Isto anonimiza a sua conta e não pode ser revertido. As encomendas mantêm-se apenas para obrigações fiscais. Continuar?')) return;
    setBusy(true);
    setMessage(null);
    try {
      await api.anonymizeMyAccount();
      saveSession(null);
      auth.logout();
      navigate('/portfolio');
      setMessage('Conta anonimizada.');
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Não foi possível anonimizar.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="page">
      <h1>Privacidade e dados</h1>
      <p className="muted">Direitos do titular (RGPD): portabilidade e eliminação/anonimização da conta, isolados por loja.</p>
      <div className="card form stack">
        <button type="button" onClick={exportData} disabled={busy}>
          Exportar os meus dados (JSON)
        </button>
        <button type="button" className="secondary" onClick={anonymize} disabled={busy}>
          Pedir anonimização da conta
        </button>
        {message && <p className="muted">{message}</p>}
      </div>
    </section>
  );
}
