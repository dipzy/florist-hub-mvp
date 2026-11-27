import { Check, Monitor, Moon, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { appTranslate } from '../lib/appTranslate';
import { useUiPreferences } from '../context/UiPreferencesContext';
import type { UiThemePreference } from '../lib/uiAppearance';

type Props = { className?: string; compact?: boolean };

const THEME_OPTIONS: { id: UiThemePreference; icon: typeof Sun }[] = [
  { id: 'light', icon: Sun },
  { id: 'dark', icon: Moon },
  { id: 'system', icon: Monitor },
];

export function AppearanceSelect({ className }: Props) {
  const { t } = useTranslation();
  const { theme, setTheme } = useUiPreferences();

  return (
    <div className={className ?? 'fh-theme-select'}>
      <p className="store-prefs-menu__heading">{t('preferences.appearance.label')}</p>
      <div className="store-prefs-menu__options" role="radiogroup" aria-label={t('preferences.appearance.label')}>
        {THEME_OPTIONS.map((opt) => {
          const Icon = opt.icon;
          const active = theme === opt.id;
          return (
            <button
              key={opt.id}
              type="button"
              role="radio"
              aria-checked={active}
              className="fh-btn store-prefs-menu__option"
              onClick={() => setTheme(opt.id)}
            >
              <Icon size={16} aria-hidden />
              <span>{appTranslate(`preferences.appearance.${opt.id}`, t)}</span>
              {active ? <Check size={16} aria-hidden /> : <span className="store-prefs-menu__check" />}
            </button>
          );
        })}
      </div>
    </div>
  );
}
