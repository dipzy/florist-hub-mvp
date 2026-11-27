import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { onCookieConsentChanged, initSentryIfAllowed } from '../lib/sentryInit';
import { readCookieConsent, saveCookieConsent } from '../lib/cookieConsent';

export function CookieBanner() {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (!readCookieConsent()) setVisible(true);
    else initSentryIfAllowed();
  }, []);

  if (!visible) return null;

  function choose(choice: 'essential' | 'all') {
    saveCookieConsent(choice);
    onCookieConsentChanged();
    setVisible(false);
  }

  return (
    <div className="cookie-banner" role="dialog" aria-label={t('cookies.bannerAria')}>
      <div className="cookie-banner__inner card">
        <p>
          {t('cookies.message')}{' '}
          <Link to="/legal/cookies">{t('cookies.policyLink')}</Link>.
        </p>
        <div className="actions row">
          <button type="button" className="secondary" onClick={() => choose('essential')}>
            {t('cookies.essentialOnly')}
          </button>
          <button type="button" onClick={() => choose('all')}>
            {t('cookies.acceptAll')}
          </button>
        </div>
      </div>
    </div>
  );
}
