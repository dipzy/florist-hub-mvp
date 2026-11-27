import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import i18n, { i18nReady } from '../i18n';
import pt from '../i18n/locales/pt-PT.json';
import en from '../i18n/locales/en.json';
import { flattenTranslationStrings } from './localeFlat';
import {
  hasUnresolvedPlaceholders,
  mockPlaceholderValues,
  translationPlaceholders,
} from './i18nPlaceholders';

const SRC_ROOT = join(import.meta.dirname, '..');

function listSourceFiles(dir: string): string[] {
  const out: string[] = [];
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    const stat = statSync(path);
    if (stat.isDirectory()) {
      if (name === 'node_modules' || name === 'locales') continue;
      out.push(...listSourceFiles(path));
      continue;
    }
    if (/\.(tsx?|jsx?)$/.test(name) && !/\.test\./.test(name)) out.push(path);
  }
  return out;
}

describe('locale placeholders', () => {
  const fpt = flattenTranslationStrings(pt as never);
  const fen = flattenTranslationStrings(en as never);
  const placeholderKeys = Object.entries(fpt)
    .filter(([, value]) => hasUnresolvedPlaceholders(value))
    .map(([key]) => key);

  it('pt-PT e en partilham as mesmas chaves com placeholders', () => {
    const enWithPh = Object.entries(fen)
      .filter(([, value]) => hasUnresolvedPlaceholders(value))
      .map(([key]) => key)
      .sort();
    expect(placeholderKeys.sort()).toEqual(enWithPh);
  });

  it.each(placeholderKeys)('i18n.t("%s") com parâmetros mock não deixa {{', async (key) => {
    await i18nReady;
    const template = fpt[key];
    const params = mockPlaceholderValues(translationPlaceholders(template));
    for (const lng of ['pt-PT', 'en'] as const) {
      await i18n.changeLanguage(lng);
      const result = i18n.t(key, params);
      expect(result, `chave ${key} (${lng})`).not.toMatch(/\{\{\w+\}\}/);
      for (const name of translationPlaceholders(template)) {
        expect(result).toContain(`__${name}__`);
      }
    }
  });

  it('nenhuma chamada t("chave") em falta de opções para chaves com placeholders', () => {
    const placeholderSet = new Set(placeholderKeys);
    const bareCall = /\bt\s*\(\s*['"]([^'"]+)['"]\s*\)/g;
    const violations: string[] = [];

    for (const file of listSourceFiles(SRC_ROOT)) {
      const rel = file.slice(SRC_ROOT.length + 1);
      const content = readFileSync(file, 'utf8');
      let match = bareCall.exec(content);
      while (match) {
        const key = match[1];
        if (placeholderSet.has(key)) violations.push(`${rel}: t('${key}')`);
        match = bareCall.exec(content);
      }
      bareCall.lastIndex = 0;
    }

    expect(violations, violations.join('\n')).toEqual([]);
  });
});
