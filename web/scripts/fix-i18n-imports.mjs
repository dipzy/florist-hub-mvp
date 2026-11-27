import fs from 'fs';
import path from 'path';

const srcRoot = path.join(process.cwd(), 'src');

const skipUsePageText = new Set([
  'main.tsx',
  'components/LanguageSelect.tsx',
  'components/AppearanceSelect.tsx',
  'components/UiPreferencesPanel.tsx',
  'components/Layout.tsx',
  'components/AdminLayout.tsx',
  'components/SuperAdminLayout.tsx',
  'components/CookieBanner.tsx',
  'components/SessionAccountFooter.tsx',
  'components/ui/LoadingState.tsx',
  'components/ui/ErrorState.tsx',
  'App.tsx',
  'context/UiPreferencesContext.tsx',
  'context/ToastContext.tsx',
  'configurator/ArrangementScene.tsx',
  'configurator/FlowerMesh.tsx',
  'configurator/InstancedBouquet.tsx',
]);

function walk(dir, files = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) walk(p, files);
    else if (ent.name.endsWith('.tsx') || ent.name.endsWith('.ts')) files.push(p);
  }
  return files;
}

for (const abs of walk(srcRoot)) {
  const rel = path.relative(srcRoot, abs);
  if (rel.endsWith('.test.ts') || rel.endsWith('.test.tsx')) continue;
  let text = fs.readFileSync(abs, 'utf8');
  let changed = false;

  if (skipUsePageText.has(rel)) {
    text = text.replace(/\nimport \{ usePageText \} from ['"][^'"]+['"];\n/g, '\n');
    text = text.replace(/\n  const \{ t, pt \} = usePageText\([^)]+\);\n/g, '\n');
    changed = true;
  } else if (text.includes('usePageText') || text.includes(' pt(') || text.includes('{pt(')) {
    const depth = rel.split('/').length - 1;
    const importPath = `${'../'.repeat(depth)}lib/usePageText`;
    text = text.replace(/import \{ usePageText \} from ['"][^'"]+['"];\n/g, '');
    if (!text.includes("from '" + importPath) && !text.includes('from "' + importPath)) {
      const hook = `import { usePageText } from '${importPath}';\n`;
      if (text.match(/from ['"]react['"]/)) {
        text = text.replace(/(from ['"]react(?:\/[^'"]+)?['"];\n)/, `$1${hook}`);
      } else {
        text = hook + text;
      }
    }
    changed = true;
  }

  if (text.match(/const \{ t, pt \} = usePageText/)) {
    // remove duplicate useTranslation t if both exist
    const lines = text.split('\n');
    const tDecl = lines.filter((l) => l.includes('const { t') || l.includes('const {t'));
    if (tDecl.length > 1) {
      text = text.replace(/\n  const \{ t \} = useTranslation\(\);\n/g, '\n');
    }
  }

  if (changed) fs.writeFileSync(abs, text);
}

console.log('Fixed imports');
