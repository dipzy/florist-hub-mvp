import { useTranslation } from 'react-i18next';
import { AppearanceSelect } from './AppearanceSelect';
import { LanguageSelect } from './LanguageSelect';

export function UiPreferencesPanel() {
  const { t } = useTranslation();

  return (
    <section className="card account-preferences" aria-labelledby="ui-preferences-heading">
      <h2 id="ui-preferences-heading" className="account-profile__section-title">
        {t('preferences.sectionTitle')}
      </h2>
      <p className="muted">{t('preferences.sectionDescription')}</p>
      <div className="account-preferences__grid">
        <LanguageSelect />
        <AppearanceSelect />
      </div>
    </section>
  );
}
