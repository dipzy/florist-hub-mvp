export const FLOWER_KINDS = [
  'rose',
  'tulip',
  'sunflower',
  'daisy',
  'lily',
  'hydrangea',
  'alstroemeria',
  'foliage',
  'generic',
] as const;

export type FlowerKind = (typeof FLOWER_KINDS)[number];

/**
 * Modelos procedurais incluídos. `modelUrls` pode substituir um destes por um GLB
 * local mais tarde; não há modelos de terceiros no repositório.
 */
export function flowerKindFromName(name: string): FlowerKind {
  const n = name
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '');

  if (n.includes('eucalipto') || n.includes('folhagem') || n.includes('ruscus') || n.includes('gipsofila') || n.includes('feto') || n.includes('hera')) {
    return 'foliage';
  }
  if (n.includes('hortensia') || n.includes('hydrangea')) return 'hydrangea';
  if (n.includes('alstroemeria') || n.includes('alstroemelia') || n.includes('astromelia')) return 'alstroemeria';
  if (n.includes('rosa') || n.includes('rose')) return 'rose';
  if (n.includes('tulipa') || n.includes('tulip')) return 'tulip';
  if (n.includes('girassol') || n.includes('sunflower')) return 'sunflower';
  if (n.includes('margarida') || n.includes('gerbera') || n.includes('daisy')) return 'daisy';
  if (n.includes('lirio') || n.includes('lily') || n.includes('acucena') || n.includes('orquid')) return 'lily';
  return 'generic';
}
