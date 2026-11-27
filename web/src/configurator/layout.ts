import { resolveFlowerColor } from './colors';
import { flowerKindFromName, type FlowerKind } from './flowerKind';
import { mulberry32 } from './prng';

/** Limite visual para o telemóvel e um portátil normal. As quantidades reais da encomenda não mudam. */
export const MAX_STEMS = 18;

export type ArrangementShapeId = 0 | 1 | 2;
export type ArrangementBaseId = 0 | 1 | 2;

export type Vec3 = { x: number; y: number; z: number };
export type Quat = { x: number; y: number; z: number; w: number };

export type StemInput = {
  name: string;
  quantity: number;
  color?: string | null;
};

export type PlacedStem = {
  kind: FlowerKind;
  color: string;
  name: string;
  head: Vec3;
  base: Vec3;
  quaternion: Quat;
  scale: number;
  leaf: number;
};

export type FoliageTuft = {
  position: Vec3;
  scale: number;
  rotationY: number;
};

export type ArrangementLayout = {
  seed: number;
  shape: ArrangementShapeId;
  baseType: ArrangementBaseId;
  stems: PlacedStem[];
  foliage: FoliageTuft[];
  height: number;
  radius: number;
};

const GOLDEN = Math.PI * (3 - Math.sqrt(5));

/** Formato cúpula/esponja: sempre base de esponja, sem jarra. */
export function shapeAllowsVase(shape: ArrangementShapeId): boolean {
  return shape !== 0;
}

export function resolveBaseType(shape: ArrangementShapeId, withVase: boolean): ArrangementBaseId {
  if (shape === 0) return 1;
  if (withVase) return 0;
  return 2;
}

export function allocateQuantities(items: StemInput[], max = MAX_STEMS) {
  const qty = items.map((item) => Math.max(0, Math.floor(item.quantity)));
  const total = qty.reduce((sum, n) => sum + n, 0);
  if (total <= max) return qty;

  const active = qty.map((n, index) => ({ index, n })).filter((entry) => entry.n > 0);
  if (active.length >= max) {
    const capped = qty.map(() => 0);
    active.slice(0, max).forEach((entry) => {
      capped[entry.index] = 1;
    });
    return capped;
  }

  const base: number[] = qty.map((n) => (n > 0 ? 1 : 0));
  const remaining = max - base.reduce((sum, n) => sum + n, 0);
  const weights = qty.map((n) => Math.max(0, n - 1));
  const weightSum = weights.reduce((sum, n) => sum + n, 0);
  if (weightSum === 0 || remaining <= 0) return base;

  const raw = weights.map((weight) => (weight / weightSum) * remaining);
  const extra = raw.map((value) => Math.floor(value));
  let used = extra.reduce((sum, n) => sum + n, 0);
  const fractions = raw
    .map((value, index) => ({ index, fraction: value - extra[index] }))
    .sort((a, b) => b.fraction - a.fraction || a.index - b.index);

  let cursor = 0;
  while (used < remaining) {
    extra[fractions[cursor % fractions.length].index] += 1;
    used += 1;
    cursor += 1;
  }

  return base.map((n, index) => n + extra[index]);
}

function quatFromUnitVectors(from: Vec3, to: Vec3): Quat {
  const r = from.x * to.x + from.y * to.y + from.z * to.z + 1;
  if (r < 1e-6) {
    if (Math.abs(from.x) > Math.abs(from.z)) {
      return normalizeQuat({ x: -from.y, y: from.x, z: 0, w: 0 });
    }
    return normalizeQuat({ x: 0, y: -from.z, z: from.y, w: 0 });
  }
  return normalizeQuat({
    x: from.y * to.z - from.z * to.y,
    y: from.z * to.x - from.x * to.z,
    z: from.x * to.y - from.y * to.x,
    w: r,
  });
}

function normalizeQuat(q: Quat): Quat {
  const len = Math.hypot(q.x, q.y, q.z, q.w) || 1;
  return { x: q.x / len, y: q.y / len, z: q.z / len, w: q.w / len };
}

function stemQuaternion(base: Vec3, head: Vec3): Quat {
  const dx = head.x - base.x;
  const dy = head.y - base.y;
  const dz = head.z - base.z;
  const len = Math.hypot(dx, dy, dz) || 1;
  return quatFromUnitVectors({ x: 0, y: 1, z: 0 }, { x: dx / len, y: dy / len, z: dz / len });
}

export function generateArrangement(input: {
  seed: number;
  shape: ArrangementShapeId;
  baseType: ArrangementBaseId;
  items: StemInput[];
}): ArrangementLayout {
  const shape = input.shape;
  const baseType = input.baseType;
  const withVase = baseType === 0;
  const counts = allocateQuantities(input.items);
  const specs: { name: string; color: string; kind: FlowerKind }[] = [];

  input.items.forEach((item, index) => {
    const count = counts[index] ?? 0;
    const kind = flowerKindFromName(item.name);
    const color = resolveFlowerColor(item.color, item.name);
    for (let i = 0; i < count; i += 1) {
      specs.push({ name: item.name, color, kind });
    }
  });

  const rand = mulberry32(input.seed);
  const stems: PlacedStem[] = specs.map((spec, index) => {
    const jitter = rand();
    const radiusJitter = rand();
    const scale = 0.88 + rand() * 0.22;
    const leaf = rand() * Math.PI * 2;
    const count = specs.length;
    const t = count <= 1 ? 0.55 : index / (count - 1);
    const theta = index * GOLDEN + jitter * 0.35;
    const lift = withVase ? 0.12 : 0;

    let x = 0;
    let y = 0.4;
    let z = 0;
    let baseSpread = 0.08;

    if (shape === 1) {
      const radius = 0.08 + t * 0.05 + radiusJitter * 0.025;
      x = Math.cos(theta) * radius;
      z = Math.sin(theta) * radius;
      y = 0.4 + t * 0.78 + lift;
      baseSpread = 0.04;
    } else if (shape === 2) {
      const radius = 0.1 + Math.sqrt(t) * 0.16 + radiusJitter * 0.02;
      x = Math.cos(theta) * radius;
      z = Math.sin(theta) * radius * 0.9;
      y = 0.36 + t * 0.2 + jitter * 0.02 + lift;
      baseSpread = 0.1;
    } else {
      const yNorm = 0.22 + t * 0.78;
      const ring = Math.sqrt(Math.max(0, 1 - yNorm * yNorm));
      const radius = 0.36 + radiusJitter * 0.05;
      x = Math.cos(theta) * ring * radius;
      z = Math.sin(theta) * ring * radius;
      y = 0.3 + yNorm * 0.46 + lift;
      baseSpread = 0.12;
    }

    const base = {
      x: x * baseSpread,
      y: (withVase ? 0.2 : 0.07) + (spec.kind === 'foliage' ? -0.01 : 0),
      z: z * baseSpread,
    };
    const head = { x, y: spec.kind === 'foliage' ? y * 0.72 : y, z };
    return {
      kind: spec.kind,
      color: spec.color,
      name: spec.name,
      head,
      base,
      quaternion: stemQuaternion(base, head),
      scale: spec.kind === 'sunflower' ? scale * 1.12 : scale,
      leaf,
    };
  });

  const foliageCount = stems.length === 0 ? 0 : shape === 2 ? 5 : shape === 1 ? 6 : 8;
  const foliage: FoliageTuft[] = [];
  for (let i = 0; i < foliageCount; i += 1) {
    const angle = rand() * Math.PI * 2;
    const radius = 0.1 + rand() * (shape === 1 ? 0.08 : 0.16);
    const scale = 0.7 + rand() * 0.5;
    foliage.push({
      position: {
        x: Math.cos(angle) * radius,
        y: withVase ? 0.22 : 0.1,
        z: Math.sin(angle) * radius,
      },
      scale,
      rotationY: angle,
    });
  }

  const height = stems.reduce((max, stem) => Math.max(max, stem.head.y), withVase ? 0.34 : 0.2);
  const radius = stems.reduce((max, stem) => Math.max(max, Math.hypot(stem.head.x, stem.head.z)), 0.2);

  return {
    seed: input.seed,
    shape,
    baseType,
    stems,
    foliage,
    height,
    radius,
  };
}
