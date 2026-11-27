export type UiThemePreference = 'light' | 'dark' | 'system';

const THEME_KEY = 'floristhub.uiTheme';
const RESOLVED_ATTR = 'data-fh-resolved-theme';

const media =
  typeof window !== 'undefined' && typeof window.matchMedia === 'function'
    ? window.matchMedia('(prefers-color-scheme: dark)')
    : null;

function readStoredTheme(): UiThemePreference {
  const raw = localStorage.getItem(THEME_KEY);
  if (raw === 'light' || raw === 'dark' || raw === 'system') return raw;
  return 'system';
}

export function getUiThemePreference(): UiThemePreference {
  return readStoredTheme();
}

export function setUiThemePreference(theme: UiThemePreference): void {
  localStorage.setItem(THEME_KEY, theme);
  applyResolvedTheme(resolveTheme(theme));
}

export function resolveTheme(preference: UiThemePreference): 'light' | 'dark' {
  if (preference === 'light' || preference === 'dark') return preference;
  return media?.matches ? 'dark' : 'light';
}

export function applyResolvedTheme(resolved: 'light' | 'dark'): void {
  document.documentElement.setAttribute(RESOLVED_ATTR, resolved);
  document.documentElement.style.colorScheme = resolved;
  document.documentElement.classList.toggle('fh-theme-dark', resolved === 'dark');
  document.documentElement.classList.toggle('fh-theme-light', resolved === 'light');
}

export function initUiAppearance(): () => void {
  const preference = readStoredTheme();
  applyResolvedTheme(resolveTheme(preference));

  if (!media) return () => undefined;

  const onChange = () => {
    if (readStoredTheme() === 'system') applyResolvedTheme(resolveTheme('system'));
  };
  media.addEventListener('change', onChange);
  return () => media.removeEventListener('change', onChange);
}
