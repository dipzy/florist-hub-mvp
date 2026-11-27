/** WCAG relative luminance and contrast helpers for store theme colours. */

export type Rgb = { r: number; g: number; b: number };

export function parseHexColor(hex: string): Rgb | null {
  const raw = hex.trim().replace(/^#/, '');
  if (raw.length === 3) {
    const r = parseInt(raw[0] + raw[0], 16);
    const g = parseInt(raw[1] + raw[1], 16);
    const b = parseInt(raw[2] + raw[2], 16);
    if ([r, g, b].some((n) => Number.isNaN(n))) return null;
    return { r, g, b };
  }
  if (raw.length === 6 || raw.length === 8) {
    const r = parseInt(raw.slice(0, 2), 16);
    const g = parseInt(raw.slice(2, 4), 16);
    const b = parseInt(raw.slice(4, 6), 16);
    if ([r, g, b].some((n) => Number.isNaN(n))) return null;
    return { r, g, b };
  }
  return null;
}

function channelLuminance(c: number): number {
  const s = c / 255;
  return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
}

export function relativeLuminance(rgb: Rgb): number {
  return 0.2126 * channelLuminance(rgb.r) + 0.7152 * channelLuminance(rgb.g) + 0.0722 * channelLuminance(rgb.b);
}

export function contrastRatio(a: number, b: number): number {
  const lighter = Math.max(a, b);
  const darker = Math.min(a, b);
  return (lighter + 0.05) / (darker + 0.05);
}

const INK = '#1c2420';
const WHITE = '#ffffff';

export function pickReadableTextColor(backgroundHex: string, dark = INK, light = WHITE): string {
  const rgb = parseHexColor(backgroundHex);
  if (!rgb) return light;
  const bgLum = relativeLuminance(rgb);
  const darkRgb = parseHexColor(dark)!;
  const lightRgb = parseHexColor(light)!;
  const darkContrast = contrastRatio(bgLum, relativeLuminance(darkRgb));
  const lightContrast = contrastRatio(bgLum, relativeLuminance(lightRgb));
  return darkContrast >= lightContrast ? dark : light;
}

/** Picks text on `background` meeting at least AA (4.5:1) when possible. */
export function pickOnColorWcag(backgroundHex: string): string {
  const choice = pickReadableTextColor(backgroundHex);
  const rgb = parseHexColor(backgroundHex);
  if (!rgb) return WHITE;
  const bgLum = relativeLuminance(rgb);
  const chosenLum = relativeLuminance(parseHexColor(choice)!);
  if (contrastRatio(bgLum, chosenLum) >= 4.5) return choice;
  return choice === WHITE ? INK : WHITE;
}

/** Accent used as foreground on the default page background (cream). */
export function pickAccentOnSurface(accentHex: string, surfaceHex = '#fffcf7'): string {
  const accentRgb = parseHexColor(accentHex);
  const surfaceRgb = parseHexColor(surfaceHex);
  if (!accentRgb || !surfaceRgb) return accentHex;
  const aLum = relativeLuminance(accentRgb);
  const sLum = relativeLuminance(surfaceRgb);
  if (contrastRatio(aLum, sLum) >= 4.5) return accentHex;
  const darker = pickOnColorWcag(accentHex) === WHITE ? '#6b4549' : accentHex;
  const darkerRgb = parseHexColor(darker);
  if (darkerRgb && contrastRatio(relativeLuminance(darkerRgb), sLum) >= 4.5) return darker;
  return '#8a4a55';
}

export function focusRingRgba(backgroundHex: string, alpha = 0.35): string {
  const rgb = parseHexColor(backgroundHex);
  if (!rgb) return `rgba(74, 135, 112, ${alpha})`;
  return `rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${alpha})`;
}
