const STORAGE_KEY = 'floristhub.cookieConsent';

export type CookieConsentChoice = 'essential' | 'all';

export function readCookieConsent(): CookieConsentChoice | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (raw === 'essential' || raw === 'all') return raw;
  return null;
}

export function saveCookieConsent(choice: CookieConsentChoice) {
  localStorage.setItem(STORAGE_KEY, choice);
}

export function analyticsCookiesAllowed(): boolean {
  return readCookieConsent() === 'all';
}
