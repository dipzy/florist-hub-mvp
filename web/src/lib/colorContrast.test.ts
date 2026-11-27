import { describe, expect, it } from 'vitest';
import { contrastRatio, parseHexColor, pickAccentOnSurface, pickOnColorWcag, relativeLuminance } from './colorContrast';

describe('colorContrast', () => {
  it('parses hex', () => {
    expect(parseHexColor('#fff')).toEqual({ r: 255, g: 255, b: 255 });
    expect(parseHexColor('#2d6a4f')).toEqual({ r: 45, g: 106, b: 79 });
  });

  it('meets AA on dark and light store primaries', () => {
    for (const hex of ['#2d6a4f', '#f4f8f6', '#fdf6f7', '#ffffff', '#356b58']) {
      const rgb = parseHexColor(hex)!;
      const bgLum = relativeLuminance(rgb);
      const on = pickOnColorWcag(hex);
      const onLum = relativeLuminance(parseHexColor(on)!);
      expect(contrastRatio(bgLum, onLum)).toBeGreaterThanOrEqual(4.5);
    }
  });

  it('darkens very light accents on cream background', () => {
    expect(pickAccentOnSurface('#fdf6f7')).not.toBe('#fdf6f7');
    const accentRgb = parseHexColor(pickAccentOnSurface('#f4f8f6'))!;
    const surfaceRgb = parseHexColor('#fffcf7')!;
    expect(contrastRatio(relativeLuminance(accentRgb), relativeLuminance(surfaceRgb))).toBeGreaterThanOrEqual(4.5);
  });
});
