export const LOCALE_STORAGE_KEY = 'floristhub.locale';

export const SUPPORTED_LOCALES = ['pt-PT', 'en'] as const;
export type AppLocale = (typeof SUPPORTED_LOCALES)[number];

export function isAppLocale(value: string): value is AppLocale {
  return SUPPORTED_LOCALES.includes(value as AppLocale);
}
