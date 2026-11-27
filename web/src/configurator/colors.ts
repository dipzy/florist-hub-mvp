const NAMED: Record<string, string> = {
  vermelho: '#c23b4a',
  vermelha: '#c23b4a',
  rosa: '#e48cae',
  'cor-de-rosa': '#e48cae',
  cordeRosa: '#e48cae',
  amarelo: '#f0c14a',
  amarela: '#f0c14a',
  branco: '#f7f4ef',
  branca: '#f7f4ef',
  roxo: '#7a4e9a',
  roxa: '#7a4e9a',
  lilas: '#b89ad4',
  laranja: '#e07a3d',
  azul: '#3d6ea8',
  verde: '#5e8f62',
  creme: '#f3e6c8',
  pessego: '#f0b48a',
  coral: '#e56b6f',
  fucsia: '#c2185b',
  fuchsia: '#c2185b',
  magenta: '#c2185b',
  'vermelho-rosado': '#9d2340',
  vermelhorosado: '#9d2340',
  vinho: '#7a2430',
  borgonha: '#7a2430',
  bordeaux: '#7a2430',
  salmao: '#e9967a',
};

const DEFAULT_BLOOM = '#d45d79';

export function normalizeColorKey(value: string) {
  return value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .replace(/\s+/g, ' ');
}

export function shadeHex(hex: string, percent: number) {
  const n = hex.replace('#', '');
  const num = Number.parseInt(n.length === 3 ? n.split('').map((c) => c + c).join('') : n, 16);
  const channels = [(num >> 16) & 255, (num >> 8) & 255, num & 255];
  const target = percent < 0 ? 0 : 255;
  const p = Math.min(1, Math.abs(percent) / 100);
  const next = channels.map((channel) => Math.round((target - channel) * p + channel));
  return `#${next.map((channel) => channel.toString(16).padStart(2, '0')).join('')}`;
}

function hashColor(key: string) {
  let hash = 2166136261;
  for (let i = 0; i < key.length; i += 1) {
    hash ^= key.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  const hue = (hash >>> 0) % 360;
  return hslToHex(hue, 58, 58);
}

function hslToHex(h: number, s: number, l: number) {
  const sat = s / 100;
  const light = l / 100;
  const k = (n: number) => (n + h / 30) % 12;
  const a = sat * Math.min(light, 1 - light);
  const f = (n: number) => light - a * Math.max(-1, Math.min(k(n) - 3, Math.min(9 - k(n), 1)));
  const channels = [f(0), f(8), f(4)].map((channel) => Math.round(channel * 255));
  return `#${channels.map((channel) => channel.toString(16).padStart(2, '0')).join('')}`;
}

export function resolveFlowerColor(input?: string | null, nameHint?: string) {
  if (input && input.trim()) {
    const raw = input.trim();
    const hex = raw.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/i);
    if (hex) {
      const body = hex[1].length === 3 ? hex[1].split('').map((c) => c + c).join('') : hex[1];
      return `#${body.toLowerCase()}`;
    }
    const key = normalizeColorKey(raw);
    if (NAMED[key]) return NAMED[key];
    const first = key.split(/[\s/,-]+/)[0] ?? key;
    if (NAMED[first]) return NAMED[first];
    return hashColor(key);
  }

  if (nameHint) {
    const name = normalizeColorKey(nameHint);
    if (name.includes('girassol')) return NAMED.amarelo;
    if (name.includes('alstroemeria')) return NAMED.fucsia;
    if (name.includes('fucsia')) return NAMED.fucsia;
    if (name.includes('hortensia')) return '#7eb0d6';
    if (name.includes('lavanda')) return NAMED.lilas;
    if (name.includes('margarida') || name.includes('gerbera')) return NAMED.branco;
    if (name.includes('eucalipto') || name.includes('folhagem')) return NAMED.verde;
    if (name.includes('tulipa')) return '#e24b6a';
    if (name.includes('lirio')) return '#f4efe6';
  }

  return DEFAULT_BLOOM;
}

/** Manchas da alstroeméria: magenta escuro, distinto do vermelho da rosa carmim (#9d2340). */
export const ALSTROEMERIA_SPOT = '#5a1244';
