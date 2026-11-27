import { useTranslation } from 'react-i18next';
import { Button } from './Button';

type Props = {
  title?: string;
  message: string;
  onRetry?: () => void;
};

export function ErrorState({ title, message, onRetry }: Props) {
  const { t } = useTranslation();
  return (
    <div className="fh-error-state" role="alert">
      <h3>{title ?? t('errors.loadFailedTitle')}</h3>
      <p>{message}</p>
      {onRetry && (
        <Button type="button" variant="secondary" size="sm" onClick={onRetry}>
          {t('common.retry')}
        </Button>
      )}
    </div>
  );
}
