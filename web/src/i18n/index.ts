import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import ptPT from './locales/pt-PT.json';
import en from './locales/en.json';
import { isAppLocale, LOCALE_STORAGE_KEY, type AppLocale } from '../lib/localeConstants';
import { hasUnresolvedPlaceholders } from '../lib/i18nPlaceholders';

function detectInitialLocale(): AppLocale {
  try {
    const stored = localStorage.getItem(LOCALE_STORAGE_KEY);
    if (stored && isAppLocale(stored)) return stored;
  } catch {
    /* private mode */
  }
  return 'pt-PT';
}

export function getStoredLocale(): AppLocale {
  try {
    const stored = localStorage.getItem(LOCALE_STORAGE_KEY);
    if (stored && isAppLocale(stored)) return stored;
  } catch {
    /* ignore */
  }
  return 'pt-PT';
}

export function setStoredLocale(locale: AppLocale): void {
  localStorage.setItem(LOCALE_STORAGE_KEY, locale);
}

const initPromise = i18n.use(initReactI18next).init({
  resources: {
    'pt-PT': { translation: ptPT },
    en: { translation: en },
  },
  lng: detectInitialLocale(),
  fallbackLng: {
    pt: ['pt-PT'],
    'pt-BR': ['pt-PT'],
    default: ['pt-PT'],
  },
  supportedLngs: ['pt-PT', 'en'],
  nonExplicitSupportedLngs: true,
  defaultNS: 'translation',
  ns: ['translation'],
  preload: ['pt-PT', 'en'],
  returnEmptyString: false,
  interpolation: { escapeValue: false },
  react: {
    useSuspense: false,
  },
});

function translationOptions(args: Parameters<typeof i18n.t>): Record<string, unknown> {
  const second = args[1];
  if (second && typeof second === 'object' && !Array.isArray(second)) {
    return second as Record<string, unknown>;
  }
  return {};
}

function interpolateLeaf(leaf: string, options: Record<string, unknown>, lng: string): string {
  if (!hasUnresolvedPlaceholders(leaf) || Object.keys(options).length === 0) return leaf;
  return i18n.services.interpolator.interpolate(leaf, options, lng, i18n.options.interpolation ?? {}) as string;
}

function patchTranslateFallback() {
  const baseT = i18n.t.bind(i18n);
  const patched = ((...args: Parameters<typeof i18n.t>) => {
    const key = args[0];
    const k = Array.isArray(key) ? key[0] : key;
    const options = translationOptions(args);
    const lng = i18n.resolvedLanguage ?? i18n.language;
    let out = baseT(...args);
    if (typeof out === 'string' && hasUnresolvedPlaceholders(out) && Object.keys(options).length > 0) {
      out = interpolateLeaf(out, options, lng);
    }
    if (typeof k === 'string' && out === k) {
      const leaf = i18n.getResource(lng, 'translation', k);
      if (typeof leaf === 'string' && leaf) return interpolateLeaf(leaf, options, lng);
    }
    return out;
  }) as typeof i18n.t;
  i18n.t = patched;
}

export const i18nReady = initPromise.then(() => {
  patchTranslateFallback();
});

export default i18n;
