import { describe, expect, it } from 'vitest';
import { normalizeHexColor } from './normalizeHexColor';

describe('normalizeHexColor', () => {
  it('expands shorthand and lowercases', () => {
    expect(normalizeHexColor('8E3B5C')).toBe('#8e3b5c');
    expect(normalizeHexColor('#ABC')).toBe('#aabbcc');
  });

  it('rejects invalid', () => {
    expect(normalizeHexColor('not-a-color')).toBeNull();
  });
});
