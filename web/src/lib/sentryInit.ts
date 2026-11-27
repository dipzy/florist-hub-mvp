import * as Sentry from '@sentry/react';
import { getPublicTenantSlug } from './publicTenant';
import { analyticsCookiesAllowed } from './cookieConsent';

let sentryStarted = false;

export function initSentryIfAllowed() {
  if (sentryStarted) return;
  const sentryDsn = import.meta.env.VITE_SENTRY_DSN as string | undefined;
  if (!sentryDsn?.trim()) return;
  if (!analyticsCookiesAllowed()) return;

  const release = import.meta.env.VITE_SENTRY_RELEASE as string | undefined;
  const environment = import.meta.env.VITE_SENTRY_ENVIRONMENT as string | undefined;

  Sentry.init({
    dsn: sentryDsn.trim(),
    sendDefaultPii: false,
    release: release?.trim() || undefined,
    environment: environment?.trim() || import.meta.env.MODE,
    beforeSend(event) {
      const slug = getPublicTenantSlug();
      if (slug) event.tags = { ...event.tags, tenant: slug };
      return event;
    },
  });
  sentryStarted = true;
}

export function onCookieConsentChanged() {
  initSentryIfAllowed();
}
