import { useTranslation } from 'react-i18next';
import { TenantLink } from '../components/TenantLink';

export function NotFoundPage() {
  const { t } = useTranslation();
  return (
    <section className="page not-found">
      <h1>{t('errors.notFoundTitle', { defaultValue: 'Página não encontrada' })}</h1>
      <p className="lede muted">
        {t('errors.notFoundDescription', {
          defaultValue: 'O endereço não existe ou deixou de estar disponível.',
        })}
      </p>
      <p>
        <TenantLink to="/" className="fh-btn fh-btn--primary">
          {t('nav.store')}
        </TenantLink>
      </p>
    </section>
  );
}
