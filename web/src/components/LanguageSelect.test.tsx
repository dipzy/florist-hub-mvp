/** @vitest-environment jsdom */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { i18nReady } from '../i18n';
import { UiPreferencesProvider } from '../context/UiPreferencesContext';
import { LanguageSelect } from './LanguageSelect';

describe('LanguageSelect', () => {
  it('mostra as opções de idioma', async () => {
    await i18nReady;
    render(
      <UiPreferencesProvider>
        <LanguageSelect />
      </UiPreferencesProvider>,
    );
    const group = screen.getByRole('radiogroup', { name: /idioma|language/i });
    expect(group.querySelectorAll('[role="radio"]').length).toBeGreaterThanOrEqual(2);
    expect(group.textContent).toMatch(/português|portuguese/i);
  });
});
