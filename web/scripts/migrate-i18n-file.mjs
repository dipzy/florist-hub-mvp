import fs from 'fs';
import path from 'path';

const srcRoot = path.join(process.cwd(), 'src');
const fileArg = process.argv[2];
if (!fileArg) {
  console.error('Usage: node scripts/migrate-i18n-file.mjs src/pages/LoginPage.tsx');
  process.exit(1);
}

const abs = path.resolve(process.cwd(), fileArg);
const rel = path.relative(srcRoot, abs).replace(/\.(tsx|ts)$/, '');
const moduleRelPath = rel;
const prefix = `pages.${rel.replace(/\//g, '.')}`;

function slugify(s) {
  return s
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 48);
}

const pt = JSON.parse(fs.readFileSync(path.join(srcRoot, 'i18n/locales/pt-PT.json'), 'utf8'));

function getAtPath(obj, dotted) {
  return dotted.split('.').reduce((acc, p) => (acc && typeof acc === 'object' ? acc[p] : undefined), obj);
}

const pageStrings = getAtPath(pt, prefix);
if (!pageStrings || typeof pageStrings !== 'object') {
  console.log('No page strings for', prefix);
  process.exit(0);
}

const values = new Set(
  Object.values(pageStrings).filter((v) => typeof v === 'string' && v.length >= 2),
);
// longest first to avoid partial replacements
const sorted = [...values].sort((a, b) => b.length - a.length);

let text = fs.readFileSync(abs, 'utf8');
if (!text.includes('usePageText')) {
  const importLine = `import { usePageText } from '${rel.split('/').map(() => '..').join('/') || '.'}/lib/usePageText';\n`;
  // fix relative import depth
  const depth = rel.split('/').length;
  const relImport = `${'../'.repeat(depth)}lib/usePageText`;
  const hookImport = `import { usePageText } from '${relImport}';\n`;
  const reactHook = text.includes('from \'react\'') || text.includes('from "react"');
  if (reactHook) {
    text = text.replace(/(from ['"]react['"];?\n)/, `$1${hookImport}`);
  } else {
    text = `import { usePageText } from '${relImport}';\n` + text;
  }
  const fnMatch = text.match(/export function (\w+)/);
  if (fnMatch) {
    const fn = fnMatch[1];
    text = text.replace(
      new RegExp(`export function ${fn}\\([^)]*\\)\\s*\\{`),
      (m) => `${m}\n  const { t, pt } = usePageText('${moduleRelPath}');`,
    );
  }
}

for (const value of sorted) {
  const escaped = value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  // JSX text nodes: >value<
  text = text.replace(new RegExp(`>${escaped}<`, 'g'), `>{pt(${JSON.stringify(value)})}<`);
  // string props: ="value" or ='value'
  text = text.replace(new RegExp(`=(["'])${escaped}\\1`, 'g'), `={pt(${JSON.stringify(value)})}`);
  // object literals label: 'value'
  text = text.replace(new RegExp(`label:\\s*["']${escaped}["']`, 'g'), `label: pt(${JSON.stringify(value)})`);
  text = text.replace(new RegExp(`title:\\s*["']${escaped}["']`, 'g'), `title: pt(${JSON.stringify(value)})`);
  // standalone 'value' in setError( 'value' ) etc — careful
  text = text.replace(
    new RegExp(`setError\\((["'])${escaped}\\1\\)`, 'g'),
    `setError(pt(${JSON.stringify(value)}))`,
  );
  text = text.replace(
    new RegExp(`setMsg\\((["'])${escaped}\\1\\)`, 'g'),
    `setMsg(pt(${JSON.stringify(value)}))`,
  );
}

fs.writeFileSync(abs, text);
console.log('Migrated', fileArg, 'strings:', sorted.length);
