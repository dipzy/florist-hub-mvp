import i18n from '../i18n';
import type { TFunction } from 'i18next';
import { hasUnresolvedPlaceholders } from './i18nPlaceholders';

function interpolateLeaf(leaf: string, options: Record<string, unknown> | undefined, lng: string): string {
  if (!hasUnresolvedPlaceholders(leaf) || !options || Object.keys(options).length === 0) return leaf;
  return i18n.services.interpolator.interpolate(leaf, options, lng, i18n.options.interpolation ?? {}) as string;
}

/** Resolve texto quando `t()` devolve a própria chave (ex.: Vitest ou chaves com segmentos ambíguos). */
export function appTranslate(key: string, t: TFunction, options?: Record<string, unknown>): string {
  const translated = t(key, { ...options, defaultValue: '' });
  if (translated && translated !== key) {
    if (hasUnresolvedPlaceholders(translated) && options && Object.keys(options).length > 0) {
      const lng = i18n.resolvedLanguage ?? i18n.language ?? 'pt-PT';
      return interpolateLeaf(translated, options, lng);
    }
    return translated;
  }
  const lng = i18n.resolvedLanguage ?? i18n.language ?? 'pt-PT';
  const leaf = i18n.getResource(lng, 'translation', key);
  if (typeof leaf === 'string' && leaf) return interpolateLeaf(leaf, options, lng);
  return typeof options?.defaultValue === 'string' ? options.defaultValue : key;
}
