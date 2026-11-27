import { useEffect, useState } from 'react';
import { usePageText } from '../lib/usePageText';
import { useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { PublicLegalDocument } from '../api/types';
import { LegalMarkdown } from '../components/LegalMarkdown';
import { ErrorState } from '../components/ui/ErrorState';
import { LoadingState } from '../components/ui/LoadingState';

const titles: Record<string, string> = {
  termos: 'Termos e Condições',
  privacidade: 'Política de Privacidade',
  cookies: 'Política de Cookies',
};

export function LegalDocumentPage() {
  const { pt } = usePageText('pages/LegalDocumentPage');
  const { doc } = useParams<{ doc: string }>();
  const [document, setDocument] = useState<PublicLegalDocument | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const kind = doc ?? 'termos';
    const loader =
      kind === 'privacidade' ? api.legalPrivacy : kind === 'cookies' ? api.legalCookies : api.legalTerms;
    loader()
      .then(setDocument)
      .catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar documento'));
  }, [doc]);

  if (error) return <ErrorState message={error} />;
  if (!document) return <LoadingState message={pt("A carregar documento…")} />;

  return (
    <section className="page legal-page">
      <p className="muted">Versão {document.version}</p>
      <LegalMarkdown title={titles[doc ?? 'termos'] ?? document.title} body={document.contentMarkdown} responsible={document.responsibleSectionMarkdown} />
    </section>
  );
}
