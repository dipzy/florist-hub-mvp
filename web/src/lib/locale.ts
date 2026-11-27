import i18n from '../i18n';
import { type AppLocale, LOCALE_STORAGE_KEY } from './localeConstants';

export { LOCALE_STORAGE_KEY, SUPPORTED_LOCALES, type AppLocale, isAppLocale } from './localeConstants';

export function getActiveLocale(): AppLocale {
  const lng = i18n.language || localStorage.getItem(LOCALE_STORAGE_KEY) || 'pt-PT';
  if (lng.startsWith('en')) return 'en';
  return 'pt-PT';
}

/** BCP-47 tag for API Accept-Language header. */
export function getApiLocale(): string {
  return getActiveLocale();
}

export function getIntlLocale(): string {
  return getActiveLocale() === 'en' ? 'en-GB' : 'pt-PT';
}

export function formatMoney(amount: number, currency = 'EUR'): string {
  return new Intl.NumberFormat(getIntlLocale(), { style: 'currency', currency }).format(amount);
}

export function formatNumber(value: number, options?: Intl.NumberFormatOptions): string {
  return new Intl.NumberFormat(getIntlLocale(), options).format(value);
}

export function formatDate(
  value: Date | string | number,
  options?: Intl.DateTimeFormatOptions,
): string {
  const date = value instanceof Date ? value : new Date(value);
  return new Intl.DateTimeFormat(getIntlLocale(), options).format(date);
}

/** Data e hora sem segundos (locale activo). */
export function formatDateTime(value: Date | string | number): string {
  return formatDate(value, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
