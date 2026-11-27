import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { appTranslate } from '../lib/appTranslate';
import type { AppLocale } from '../lib/localeConstants';
import { useUiPreferences } from '../context/UiPreferencesContext';

type Props = { className?: string; compact?: boolean };

const LOCALE_LABEL_KEYS: Record<AppLocale, string> = {
  'pt-PT': 'preferences.language.portugal',
  en: 'preferences.language.english',
};

export function LanguageSelect({ className }: Props) {
  const { t } = useTranslation();
  const { locale, setLocale, supportedLocales } = useUiPreferences();

  return (
    <div className={className ?? 'fh-locale-select'}>
      <p className="store-prefs-menu__heading">{t('preferences.language.label')}</p>
      <div className="store-prefs-menu__options" role="radiogroup" aria-label={t('preferences.language.label')}>
        {supportedLocales.map((code) => {
          const active = locale === code;
          return (
            <button
              key={code}
              type="button"
              role="radio"
              aria-checked={active}
              className="fh-btn store-prefs-menu__option"
              onClick={() => setLocale(code)}
            >
              <span className="store-prefs-menu__locale">{code === 'en' ? 'EN' : 'PT'}</span>
              <span>{appTranslate(LOCALE_LABEL_KEYS[code], t)}</span>
              {active ? <Check size={16} aria-hidden /> : <span className="store-prefs-menu__check" />}
            </button>
          );
        })}
      </div>
    </div>
  );
}
