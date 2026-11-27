import { parseHexColor } from './colorContrast';

/** Normaliza para #RRGGBB em minúsculas; null se inválido. */
export function normalizeHexColor(raw: string): string | null {
  const trimmed = raw.trim();
  const withHash = trimmed.startsWith('#') ? trimmed : `#${trimmed}`;
  if (!parseHexColor(withHash)) return null;
  const body = withHash.slice(1);
  if (body.length === 3) {
    return `#${body
      .split('')
      .map((c) => c + c)
      .join('')
      .toLowerCase()}`;
  }
  return `#${body.slice(0, 6).toLowerCase()}`;
}
