import { describe, expect, it } from 'vitest';
import { ALSTROEMERIA_SPOT, resolveFlowerColor } from './colors';
import { flowerKindFromName } from './flowerKind';
import { allocateQuantities, generateArrangement, resolveBaseType, shapeAllowsVase, type StemInput } from './layout';

const mixed: StemInput[] = [
  { name: 'Rosa vermelha', quantity: 4, color: 'Vermelho' },
  { name: 'Túlipa amarela', quantity: 3, color: 'Amarelo' },
  { name: 'Girassol', quantity: 2, color: 'Amarelo' },
  { name: 'Margarida silvestre', quantity: 3, color: 'Branco' },
  { name: 'Lírio', quantity: 2, color: 'Branco' },
];

describe('generateArrangement', () => {
  it('é determinístico para a mesma semente e as mesmas opções', () => {
    const input = { seed: 48291, shape: 0 as const, baseType: 1 as const, items: mixed };
    expect(generateArrangement(input)).toEqual(generateArrangement(input));
  });

  it('muda a montagem quando a semente muda', () => {
    const first = generateArrangement({ seed: 48291, shape: 0, baseType: 1, items: mixed });
    const second = generateArrangement({ seed: 90210, shape: 0, baseType: 1, items: mixed });
    expect(second.stems.map((stem) => stem.head)).not.toEqual(first.stems.map((stem) => stem.head));
  });

  it('mantém a semente negativa estável', () => {
    const input = { seed: -42, shape: 2 as const, baseType: 2 as const, items: mixed };
    expect(generateArrangement(input)).toEqual(generateArrangement(input));
  });

  it('distribui a esponja em cúpula, o comprido em altura e o curto mais baixo', () => {
    const dome = generateArrangement({ seed: 7, shape: 0, baseType: 1, items: mixed });
    const long = generateArrangement({ seed: 7, shape: 1, baseType: 2, items: mixed });
    const short = generateArrangement({ seed: 7, shape: 2, baseType: 2, items: mixed });
    const maxY = (stems: { head: { y: number } }[]) => Math.max(...stems.map((stem) => stem.head.y));
    const spread = (stems: { head: { x: number; z: number } }[]) =>
      Math.max(...stems.map((stem) => Math.hypot(stem.head.x, stem.head.z)));

    expect(maxY(long.stems)).toBeGreaterThan(maxY(dome.stems));
    expect(maxY(short.stems)).toBeLessThan(maxY(dome.stems));
    expect(spread(dome.stems)).toBeGreaterThan(spread(long.stems));
  });

  it('levanta as hastes quando há jarra', () => {
    const foam = generateArrangement({ seed: 11, shape: 0, baseType: 1, items: mixed });
    const vase = generateArrangement({ seed: 11, shape: 0, baseType: 0, items: mixed });
    expect(vase.stems[0]?.head.y).toBeGreaterThan(foam.stems[0]?.head.y ?? 0);
    expect(vase.stems[0]?.base.y).toBeGreaterThan(foam.stems[0]?.base.y ?? 0);
  });

  it('reconhece os modelos e cai no genérico', () => {
    const layout = generateArrangement({
      seed: 3,
      shape: 0,
      baseType: 0,
      items: [
        ...mixed,
        { name: 'Eucalipto', quantity: 1, color: 'Verde' },
        { name: 'Cravo', quantity: 1, color: 'Rosa' },
      ],
    });
    const kinds = layout.stems.map((stem) => stem.kind);
    expect(kinds).toContain('rose');
    expect(kinds).toContain('tulip');
    expect(kinds).toContain('sunflower');
    expect(kinds).toContain('daisy');
    expect(kinds).toContain('lily');
    expect(kinds).toContain('foliage');
    expect(kinds).toContain('generic');
  });

  it('não desenha mais hastes do que o limite, mantendo as proporções', () => {
    const counts = allocateQuantities(
      [
        { name: 'Rosa', quantity: 10 },
        { name: 'Túlipa', quantity: 10 },
      ],
      18,
    );
    expect(counts).toEqual([9, 9]);
    const layout = generateArrangement({
      seed: 1,
      shape: 1,
      baseType: 0,
      items: [
        { name: 'Rosa', quantity: 40, color: '#c23b4a' },
        { name: 'Túlipa', quantity: 2, color: '#f0c14a' },
      ],
    });
    expect(layout.stems).toHaveLength(18);
    expect(layout.stems.filter((stem) => stem.kind === 'tulip').length).toBeGreaterThan(0);
  });
});

describe('catálogo visual', () => {
  it('mapeia nomes de flor, incluindo acentos', () => {
    expect(flowerKindFromName('Rosa vermelha')).toBe('rose');
    expect(flowerKindFromName('Túlipa amarela')).toBe('tulip');
    expect(flowerKindFromName('Girassol')).toBe('sunflower');
    expect(flowerKindFromName('Margarida silvestre')).toBe('daisy');
    expect(flowerKindFromName('Gérbera')).toBe('daisy');
    expect(flowerKindFromName('Lírio branco')).toBe('lily');
    expect(flowerKindFromName('Lavanda selvagem')).toBe('generic');
    expect(flowerKindFromName('Eucalipto')).toBe('foliage');
    expect(flowerKindFromName('Hortênsia azul')).toBe('hydrangea');
    expect(flowerKindFromName('Alstroeméria fúcsia')).toBe('alstroemeria');
  });

  it('usa a cor do catálogo e um tom estável quando o nome é desconhecido', () => {
    expect(resolveFlowerColor('Vermelho')).toBe('#c23b4a');
    expect(resolveFlowerColor('#ABC')).toBe('#aabbcc');
    expect(resolveFlowerColor('azul-noite')).toBe(resolveFlowerColor('azul-noite'));
    expect(resolveFlowerColor(null, 'Girassol')).toBe('#f0c14a');
    expect(resolveFlowerColor('Vermelho-rosado')).toBe('#9d2340');
    const alstro = resolveFlowerColor('Fúcsia', 'Alstroeméria fúcsia');
    const rose = resolveFlowerColor('Vermelho-rosado', 'Rosa carmim');
    expect(alstro).toBe('#c2185b');
    expect(rose).toBe('#9d2340');
    expect(alstro).not.toBe(rose);
    expect(ALSTROEMERIA_SPOT).not.toBe(rose);
    expect(ALSTROEMERIA_SPOT).not.toBe(alstro);
  });

  it('deriva a base a partir do formato e do vaso', () => {
    expect(resolveBaseType(0, true)).toBe(1);
    expect(resolveBaseType(0, false)).toBe(1);
    expect(resolveBaseType(1, false)).toBe(2);
    expect(resolveBaseType(2, true)).toBe(0);
  });

  it('formato esponja nunca permite jarra', () => {
    expect(shapeAllowsVase(0)).toBe(false);
    expect(shapeAllowsVase(1)).toBe(true);
    expect(shapeAllowsVase(2)).toBe(true);
  });
});
