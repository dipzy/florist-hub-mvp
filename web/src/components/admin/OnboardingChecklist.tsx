import { Link } from 'react-router-dom';
import type { OnboardingChecklistItem } from '../../api/types';

export function OnboardingChecklist({
  items,
  compact = false,
}: {
  items: OnboardingChecklistItem[];
  compact?: boolean;
}) {
  return (
    <ul className={`onboarding-checklist${compact ? ' compact' : ''}`} aria-label="Lista de verificação do onboarding">
      {items.map((item) => (
        <li key={item.key} className={item.isComplete ? 'done' : item.isRequired ? 'required' : 'optional'}>
          <span className="onboarding-check" aria-label={item.isComplete ? 'Concluído' : 'Por concluir'}>
            <span aria-hidden>{item.isComplete ? '✓' : '○'}</span>
          </span>
          <div>
            <strong>
              {item.title}
              {!compact && item.isRequired && !item.isComplete ? ' (obrigatório)' : ''}
            </strong>
            {!compact && <p className="muted">{item.description}</p>}
          </div>
          {!item.isComplete && item.adminPath && (
            <Link to={item.adminPath} className="onboarding-checklist-link">
              Completar
            </Link>
          )}
        </li>
      ))}
    </ul>
  );
}
