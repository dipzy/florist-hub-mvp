import { useEffect, useId, useRef, useState } from 'react';
import { Check, Monitor, Moon, Settings2, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { appTranslate } from '../lib/appTranslate';
import { useUiPreferences } from '../context/UiPreferencesContext';
import type { AppLocale } from '../lib/localeConstants';
import type { UiThemePreference } from '../lib/uiAppearance';

const THEME_OPTIONS: { id: UiThemePreference; icon: typeof Sun }[] = [
  { id: 'light', icon: Sun },
  { id: 'dark', icon: Moon },
  { id: 'system', icon: Monitor },
];

const LOCALE_LABEL_KEYS: Record<AppLocale, string> = {
  'pt-PT': 'preferences.language.portugal',
  en: 'preferences.language.english',
};

type Props = {
  className?: string;
  placement?: 'default' | 'sidebar';
};

export function StorePreferencesMenu({ className, placement = 'default' }: Props) {
  const { t } = useTranslation();
  const menuId = useId();
  const { theme, setTheme, locale, setLocale, supportedLocales } = useUiPreferences();
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(ev: MouseEvent) {
      if (!wrapRef.current?.contains(ev.target as Node)) setOpen(false);
    }
    function onKey(ev: KeyboardEvent) {
      if (ev.key === 'Escape') setOpen(false);
    }
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  return (
    <div
      className={['store-prefs-menu', placement === 'sidebar' ? 'store-prefs-menu--sidebar' : '', className]
        .filter(Boolean)
        .join(' ')}
      ref={wrapRef}
    >
      <button
        type="button"
        className="fh-btn store-prefs-menu__trigger"
        aria-expanded={open}
        aria-controls={menuId}
        aria-haspopup="dialog"
        aria-label={t('preferences.sectionTitle')}
        onClick={() => setOpen((v) => !v)}
      >
        <Settings2 size={18} aria-hidden />
      </button>
      {open && (
        <div id={menuId} className="store-prefs-menu__panel" role="dialog" aria-label={t('preferences.sectionTitle')}>
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
      )}
    </div>
  );
}
