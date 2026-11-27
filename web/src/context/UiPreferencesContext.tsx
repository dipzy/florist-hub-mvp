import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import i18n, { setStoredLocale } from '../i18n';
import { type AppLocale, isAppLocale, SUPPORTED_LOCALES } from '../lib/localeConstants';
import {
  getUiThemePreference,
  initUiAppearance,
  setUiThemePreference,
  type UiThemePreference,
} from '../lib/uiAppearance';

type UiPreferencesContextValue = {
  locale: AppLocale;
  setLocale: (locale: AppLocale) => void;
  theme: UiThemePreference;
  setTheme: (theme: UiThemePreference) => void;
  supportedLocales: readonly AppLocale[];
};

const UiPreferencesContext = createContext<UiPreferencesContextValue | null>(null);

function normalizeLocale(lng: string): AppLocale {
  if (lng.startsWith('en')) return 'en';
  return 'pt-PT';
}

export function UiPreferencesProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<AppLocale>(() => normalizeLocale(i18n.language));
  const [theme, setThemeState] = useState<UiThemePreference>(() => getUiThemePreference());

  useEffect(() => initUiAppearance(), []);

  useEffect(() => {
    const onLanguageChanged = (lng: string) => {
      setLocaleState(normalizeLocale(lng));
    };
    i18n.on('languageChanged', onLanguageChanged);
    return () => {
      i18n.off('languageChanged', onLanguageChanged);
    };
  }, []);

  const setLocale = useCallback((next: AppLocale) => {
    if (!isAppLocale(next)) return;
    setStoredLocale(next);
    void i18n.changeLanguage(next);
    setLocaleState(next);
  }, []);

  const setTheme = useCallback((next: UiThemePreference) => {
    setUiThemePreference(next);
    setThemeState(next);
  }, []);

  const value = useMemo(
    () => ({
      locale,
      setLocale,
      theme,
      setTheme,
      supportedLocales: SUPPORTED_LOCALES,
    }),
    [locale, setLocale, theme, setTheme],
  );

  return <UiPreferencesContext.Provider value={value}>{children}</UiPreferencesContext.Provider>;
}

export function useUiPreferences() {
  const ctx = useContext(UiPreferencesContext);
  if (!ctx) throw new Error('useUiPreferences must be used within UiPreferencesProvider');
  return ctx;
}
