import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
/** Must match scripts/build-locale-json.mjs slugify. */
export function pageTextSlug(text: string): string {
  return text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 48);
}

/** @param moduleRelPath Path under src without extension, e.g. `pages/LoginPage` or `components/Layout`. */
export function usePageText(moduleRelPath: string) {
  const { t, i18n } = useTranslation();
  const prefix = `pages.${moduleRelPath.replace(/\//g, '.')}`;
  const pt = useCallback(
    (text: string, options?: Record<string, unknown>) => {
      const key = `${prefix}.${pageTextSlug(text)}`;
      return t(key, { defaultValue: text, ...options });
    },
    [t, prefix],
  );
  return { t, pt, i18n };
}
