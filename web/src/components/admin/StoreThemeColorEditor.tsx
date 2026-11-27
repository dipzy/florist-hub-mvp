import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ColorField } from '../ui/ColorField';
import { contrastRatio, parseHexColor, pickOnColorWcag, relativeLuminance } from '../../lib/colorContrast';
import { normalizeHexColor } from '../../lib/normalizeHexColor';

export const SUGGESTED_PALETTES: { id: string; labelKey: string; primary: string; accent: string }[] = [
  { id: 'sao-flores', labelKey: 'admin.settings.paletteSaoFlores', primary: '#8e3b5c', accent: '#e8b4bc' },
  { id: 'forest', labelKey: 'admin.settings.paletteForest', primary: '#2d6a4f', accent: '#95d5b2' },
  { id: 'lavender', labelKey: 'admin.settings.paletteLavender', primary: '#6b4c7a', accent: '#d4b8e0' },
  { id: 'classic', labelKey: 'admin.settings.paletteClassic', primary: '#8a4a55', accent: '#e8c4c0' },
];

type Props = {
  primary: string;
  accent: string;
  storeName: string;
  onPrimaryChange: (value: string) => void;
  onAccentChange: (value: string) => void;
};

function lowContrastOnSurface(hex: string, surface = '#fffcf7'): boolean {
  const fg = parseHexColor(hex);
  const bg = parseHexColor(surface);
  if (!fg || !bg) return false;
  return contrastRatio(relativeLuminance(fg), relativeLuminance(bg)) < 4.5;
}

function ColorPairField({
  label,
  value,
  onChange,
  invalid,
}: {
  label: string;
  value: string;
  onChange: (next: string) => void;
  invalid: boolean;
}) {
  return <ColorField label={label} value={value} onChange={onChange} invalid={invalid} />;
}

export function StoreThemeColorEditor({ primary, accent, storeName, onPrimaryChange, onAccentChange }: Props) {
  const { t } = useTranslation();
  const primaryOk = normalizeHexColor(primary) !== null;
  const accentOk = normalizeHexColor(accent) !== null;
  const previewPrimary = normalizeHexColor(primary) ?? '#8e3b5c';
  const previewAccent = normalizeHexColor(accent) ?? '#e8b4bc';
  const btnText = pickOnColorWcag(previewPrimary);

  const showContrastWarning = useMemo(
    () => lowContrastOnSurface(previewPrimary) || lowContrastOnSurface(previewAccent),
    [previewPrimary, previewAccent],
  );

  return (
    <section className="theme-color-editor">
      <div className="theme-color-editor__fields">
        <ColorPairField
          label={t('admin.settings.primaryColor')}
          value={primary}
          onChange={onPrimaryChange}
          invalid={!primaryOk}
        />
        <ColorPairField
          label={t('admin.settings.accentColor')}
          value={accent}
          onChange={onAccentChange}
          invalid={!accentOk}
        />
      </div>

      <p className="theme-color-editor__palettes-heading">{t('admin.settings.suggestedPalettes')}</p>
      <div className="theme-color-editor__palettes">
        {SUGGESTED_PALETTES.map((palette) => (
          <button
            key={palette.id}
            type="button"
            className="theme-color-editor__palette"
            onClick={() => {
              onPrimaryChange(palette.primary);
              onAccentChange(palette.accent);
            }}
          >
            <span className="theme-color-editor__palette-label">{t(palette.labelKey)}</span>
            <span className="theme-color-editor__palette-swatches" aria-hidden>
              <span style={{ background: palette.primary }} />
              <span style={{ background: palette.accent }} />
            </span>
          </button>
        ))}
      </div>

      <div className="theme-color-editor__preview" aria-label={t('admin.settings.livePreview')}>
        <header className="theme-color-editor__preview-header" style={{ background: previewPrimary, color: btnText }}>
          <span className="theme-color-editor__preview-title">{storeName || t('session.storeFallback')}</span>
        </header>
        <button
          type="button"
          className="theme-color-editor__preview-btn"
          style={{ background: previewAccent, color: pickOnColorWcag(previewAccent) }}
        >
          {t('store.heroCtaBuild')}
        </button>
      </div>

      {showContrastWarning && (
        <p className="theme-color-editor__warn" role="status">
          {t('admin.settings.contrastWarning')}
        </p>
      )}
    </section>
  );
}
