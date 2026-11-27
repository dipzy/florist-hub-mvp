/** @vitest-environment jsdom */
import { beforeEach, describe, expect, it } from 'vitest';
import {
  applyResolvedTheme,
  getUiThemePreference,
  resolveTheme,
  setUiThemePreference,
  type UiThemePreference,
} from './uiAppearance';

describe('uiAppearance', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-fh-resolved-theme');
    document.documentElement.classList.remove('fh-theme-dark', 'fh-theme-light');
  });

  it('lê e grava preferência no localStorage', () => {
    expect(getUiThemePreference()).toBe('system');
    setUiThemePreference('dark');
    expect(getUiThemePreference()).toBe('dark');
    expect(localStorage.getItem('floristhub.uiTheme')).toBe('dark');
  });

  it('aplica tema resolvido no documentElement', () => {
    applyResolvedTheme('dark');
    expect(document.documentElement.getAttribute('data-fh-resolved-theme')).toBe('dark');
    expect(document.documentElement.classList.contains('fh-theme-dark')).toBe(true);
  });

  it('resolve tema system para light ou dark', () => {
    const options: UiThemePreference[] = ['light', 'dark', 'system'];
    for (const pref of options) {
      const resolved = resolveTheme(pref);
      expect(resolved === 'light' || resolved === 'dark').toBe(true);
    }
  });
});
