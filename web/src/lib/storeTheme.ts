import type { PublicStoreTheme } from '../api/types';
import { focusRingRgba, pickAccentOnSurface, pickOnColorWcag } from './colorContrast';

const FONT_STACKS: Record<string, string> = {
  system: 'system-ui, -apple-system, Segoe UI, Roboto, sans-serif',
  serif: 'Georgia, "Times New Roman", serif',
  rounded: 'Trebuchet MS, Verdana, sans-serif',
  classic: 'Palatino Linotype, Book Antiqua, Palatino, serif',
};

const STORE_THEME_VARS = [
  '--store-primary',
  '--store-accent',
  '--store-font',
  '--store-primary-on',
  '--store-accent-on',
  '--store-topbar-fg',
  '--store-topbar-muted',
  '--store-topbar-hover',
  '--store-topbar-border',
  '--store-focus-ring',
  '--store-accent-text',
] as const;

const THEME_CACHE_KEY = 'floristhub.storeThemeCache';

export function rememberStoreTheme(theme: PublicStoreTheme) {
  try {
    const slug = theme.slug?.trim().toLowerCase();
    if (!slug) return;
    const raw = localStorage.getItem(THEME_CACHE_KEY);
    const all = raw ? (JSON.parse(raw) as Record<string, PublicStoreTheme>) : {};
    all[slug] = theme;
    localStorage.setItem(THEME_CACHE_KEY, JSON.stringify(all));
  } catch {
    /* private mode */
  }
}

export function readCachedStoreTheme(slug: string | null | undefined): PublicStoreTheme | null {
  if (!slug) return null;
  try {
    const raw = localStorage.getItem(THEME_CACHE_KEY);
    if (!raw) return null;
    const all = JSON.parse(raw) as Record<string, PublicStoreTheme>;
    const theme = all[slug.trim().toLowerCase()];
    return theme?.themePrimaryColor ? theme : null;
  } catch {
    return null;
  }
}

export function applyCachedStoreTheme(slug: string | null | undefined) {
  const theme = readCachedStoreTheme(slug);
  if (theme) applyStoreTheme(theme);
}

export function applyStoreTheme(theme: PublicStoreTheme | null) {
  const root = document.documentElement;
  if (!theme) {
    for (const key of STORE_THEME_VARS) root.style.removeProperty(key);
    return;
  }
  rememberStoreTheme(theme);

  const primary = theme.themePrimaryColor;
  const accent = theme.themeAccentColor;

  root.style.setProperty('--store-primary', primary);
  root.style.setProperty('--store-accent', accent);
  root.style.setProperty('--store-primary-on', pickOnColorWcag(primary));
  root.style.setProperty('--store-accent-on', pickOnColorWcag(accent));

  const primaryOnTopbar = pickOnColorWcag(primary);
  const isLightBar = primaryOnTopbar === '#1c2420';
  root.style.setProperty('--store-topbar-fg', isLightBar ? 'var(--fh-ink)' : '#f4f8f6');
  root.style.setProperty('--store-topbar-muted', isLightBar ? 'var(--fh-ink-muted)' : 'rgba(244, 248, 246, 0.82)');
  root.style.setProperty(
    '--store-topbar-hover',
    isLightBar ? 'rgba(28, 36, 32, 0.06)' : 'rgba(255, 255, 255, 0.12)',
  );
  root.style.setProperty('--store-topbar-border', isLightBar ? 'var(--fh-border)' : 'rgba(255, 255, 255, 0.15)');
  root.style.setProperty('--store-focus-ring', `0 0 0 3px ${focusRingRgba(primary)}`);

  root.style.setProperty('--store-accent-text', pickAccentOnSurface(accent));

  const fontKey = theme.themeFontFamily?.toLowerCase() ?? 'system';
  root.style.setProperty('--store-font', FONT_STACKS[fontKey] ?? FONT_STACKS.system);
  window.dispatchEvent(new Event('floristhub:theme'));
}

/** For embed/configurador when only hex colours are passed in the query string. */
export function applyStoreThemeFromColors(primary?: string | null, accent?: string | null) {
  if (!primary && !accent) {
    applyStoreTheme(null);
    return;
  }
  applyStoreTheme({
    themePrimaryColor: primary ?? '#356b58',
    themeAccentColor: accent ?? '#d4a0a8',
    themeFontFamily: 'system',
    slug: '',
    name: '',
  });
}
