import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { i18nReady } from './i18n'
import './index.css'
import App from './App.tsx'
import { UiPreferencesProvider } from './context/UiPreferencesContext'
import { initSentryIfAllowed } from './lib/sentryInit'
import { applyResolvedTheme, getUiThemePreference, resolveTheme } from './lib/uiAppearance'
import { applyCachedStoreTheme } from './lib/storeTheme'

applyResolvedTheme(resolveTheme(getUiThemePreference()))
applyCachedStoreTheme(new URLSearchParams(window.location.search).get('tenant'))

initSentryIfAllowed()

void i18nReady.then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <UiPreferencesProvider>
        <App />
      </UiPreferencesProvider>
    </StrictMode>,
  )
})
