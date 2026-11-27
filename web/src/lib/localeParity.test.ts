import { describe, expect, it } from 'vitest';
import pt from '../i18n/locales/pt-PT.json';
import en from '../i18n/locales/en.json';

type JsonLeaf = string | number | boolean | null;
type JsonNode = JsonLeaf | { [key: string]: JsonNode } | JsonNode[];

function flattenStrings(obj: JsonNode, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  if (obj === null || typeof obj !== 'object' || Array.isArray(obj)) return out;
  for (const [key, value] of Object.entries(obj)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'string') out[path] = value;
    else if (value && typeof value === 'object' && !Array.isArray(value)) {
      Object.assign(out, flattenStrings(value, path));
    }
  }
  return out;
}

function isImportPath(value: string) {
  return value.startsWith('./') || value.startsWith('../');
}

const PT_MARK = /[àáâãéêíóôõúçÀÁÂÃÉÊÍÓÔÕÚÇ]/;

describe('locale parity pt-PT ↔ en', () => {
  const fpt = flattenStrings(pt as JsonNode);
  const fen = flattenStrings(en as JsonNode);

  it('has the same translation keys in both locales', () => {
    const ptKeys = Object.keys(fpt).sort();
    const enKeys = Object.keys(fen).sort();
    const missingInEn = ptKeys.filter((k) => !(k in fen));
    const missingInPt = enKeys.filter((k) => !(k in fpt));
    expect(missingInEn, `missing in en: ${missingInEn.slice(0, 12).join(', ')}`).toEqual([]);
    expect(missingInPt, `missing in pt: ${missingInPt.slice(0, 12).join(', ')}`).toEqual([]);
  });

  it('English pages.* is not a verbatim copy of pt-PT', () => {
    const bad: string[] = [];
    for (const [key, enValue] of Object.entries(fen)) {
      if (!key.startsWith('pages.')) continue;
      if (isImportPath(enValue)) {
        bad.push(`${key} (import path leaked into locales)`);
        continue;
      }
      const ptValue = fpt[key];
      if (ptValue && enValue === ptValue && PT_MARK.test(ptValue)) bad.push(key);
    }
    expect(bad, bad.slice(0, 25).join('\n')).toEqual([]);
  });
});
