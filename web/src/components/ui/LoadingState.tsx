import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

type Props = {
  message?: string;
  skeleton?: ReactNode;
};

export function LoadingState({ message, skeleton }: Props) {
  const { t } = useTranslation();
  const text = message ?? t('common.loading');
  if (skeleton) return <>{skeleton}</>;
  return (
    <p className="fh-page-status" role="status" aria-busy="true">
      <span className="fh-spinner" aria-hidden />
      {text}
    </p>
  );
}
