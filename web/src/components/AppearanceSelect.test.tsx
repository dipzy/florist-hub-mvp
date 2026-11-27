/** @vitest-environment jsdom */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { i18nReady } from '../i18n';
import { UiPreferencesProvider } from '../context/UiPreferencesContext';
import { AppearanceSelect } from './AppearanceSelect';

describe('AppearanceSelect', () => {
  it('mostra as opções de tema', async () => {
    await i18nReady;
    render(
      <UiPreferencesProvider>
        <AppearanceSelect />
      </UiPreferencesProvider>,
    );
    const group = screen.getByRole('radiogroup', { name: /tema|theme/i });
    expect(group.querySelectorAll('[role="radio"]').length).toBe(3);
    expect(group.textContent).toMatch(/claro|light/i);
  });
});
