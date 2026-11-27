import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { AuthContextOption } from '../api/types';
import { useAuth } from '../context/AuthContext';
import { homePathAfterContextSwitch } from '../lib/authContextsSession';
import { isActiveSessionContext, sessionContextLabel, shouldShowSessionSwitcher } from '../lib/sessionContextLabel';
import { Button } from '../components/ui/Button';
import { UiPreferencesPanel } from '../components/UiPreferencesPanel';

export function AccountProfilePage() {
  const { t } = useTranslation();
  const auth = useAuth();
  const navigate = useNavigate();
  const [contexts, setContexts] = useState<AuthContextOption[]>(auth.availableContexts ?? []);
  const [loadingContexts, setLoadingContexts] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [switchingId, setSwitchingId] = useState<string | null>(null);
  const [switchError, setSwitchError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoadingContexts(true);
    setLoadError(null);
    auth
      .refreshContexts()
      .then((list) => {
        if (!cancelled) setContexts(list);
      })
      .catch((err) => {
        if (!cancelled) {
          setLoadError(
            err instanceof Error ? err.message : t('account.contextsLoadError'),
          );
          setContexts(auth.availableContexts ?? []);
        }
      })
      .finally(() => {
        if (!cancelled) setLoadingContexts(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const showSwitcher = shouldShowSessionSwitcher(contexts);

  async function onSelectContext(option: AuthContextOption) {
    if (isActiveSessionContext(option, auth.role, auth.tenantId)) return;
    setSwitchError(null);
    const key = `${option.kind}-${option.tenantId ?? 'platform'}`;
    setSwitchingId(key);
    try {
      const role = await auth.switchContext(option.kind, option.tenantId ?? undefined);
      navigate(homePathAfterContextSwitch(role), { replace: true });
    } catch (err) {
      setSwitchError(err instanceof Error ? err.message : t('account.contextSwitchError'));
    } finally {
      setSwitchingId(null);
    }
  }

  return (
    <div className="page account-profile">
      <h1>{t('account.title')}</h1>
      <p className="muted">{t('account.lede')}</p>

      <UiPreferencesPanel />

      <section className="card account-profile__card" aria-labelledby="account-profile-account-heading">
        <h2 id="account-profile-account-heading" className="account-profile__section-title">
          {t('account.sectionAccount')}
        </h2>
        <dl className="account-profile__dl">
          <div>
            <dt>{t('account.email')}</dt>
            <dd>{auth.email ?? t('common.dash')}</dd>
          </div>
        </dl>
      </section>

      <section
        className="card account-profile__card"
        aria-labelledby="account-profile-contexts-heading"
        aria-busy={loadingContexts}
      >
        <h2 id="account-profile-contexts-heading" className="account-profile__section-title">
          {t('account.contextSection')}
        </h2>
        {loadingContexts && <p className="muted">{t('account.refreshingContexts')}</p>}
        {loadError && (
          <p className="error" role="alert">
            {loadError}
          </p>
        )}
        {!loadingContexts && !loadError && !showSwitcher && (
          <p className="muted">
            {t('account.singleContext')}
            {contexts[0] ? `: ${sessionContextLabel(contexts[0], t)}` : '.'}
          </p>
        )}
        {!loadingContexts && showSwitcher && (
          <ul className="account-profile__context-list">
            {contexts.map((option) => {
              const active = isActiveSessionContext(option, auth.role, auth.tenantId);
              const key = `${option.kind}-${option.tenantId ?? 'platform'}`;
              const busy = switchingId === key;
              return (
                <li key={key}>
                  <div
                    className={
                      active
                        ? 'account-profile__context-item account-profile__context-item--active'
                        : 'account-profile__context-item'
                    }
                  >
                    <span className="account-profile__context-label">{sessionContextLabel(option, t)}</span>
                    {active ? (
                      <span className="account-profile__context-badge">{t('account.activeBadge')}</span>
                    ) : (
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={switchingId !== null}
                        aria-busy={busy}
                        onClick={() => void onSelectContext(option)}
                      >
                        {busy ? t('account.switching') : t('account.switchTo')}
                      </Button>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
        {switchError && (
          <p className="error" role="alert">
            {switchError}
          </p>
        )}
      </section>
    </div>
  );
}
