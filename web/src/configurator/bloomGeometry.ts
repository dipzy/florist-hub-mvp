import { ExtrudeGeometry, Mesh, Shape, type BufferGeometry } from 'three';
import { ALSTROEMERIA_SPOT } from './colors';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { FlowerKind } from './flowerKind';

export type BloomParts = {
  petals: BufferGeometry;
  accent?: { geometry: BufferGeometry; color: string };
};

const cache = new Map<FlowerKind, BloomParts>();
let leafCache: BufferGeometry | null = null;

function merge(meshes: Mesh[]) {
  const parts = meshes.map((mesh) => {
    mesh.updateMatrix();
    const geometry = mesh.geometry.clone();
    geometry.applyMatrix4(mesh.matrix);
    return geometry;
  });
  const merged = mergeGeometries(parts, false);
  parts.forEach((geometry) => geometry.dispose());
  meshes.forEach((mesh) => mesh.geometry.dispose());
  if (!merged) throw new Error('Não foi possível fundir a geometria da flor.');
  merged.computeVertexNormals();
  return merged;
}

function petalShape(length: number, width: number) {
  const shape = new Shape();
  shape.moveTo(0, 0.004);
  shape.bezierCurveTo(width * 0.55, length * 0.18, width, length * 0.62, width * 0.15, length * 0.92);
  shape.bezierCurveTo(width * 0.04, length, 0, length, 0, length);
  shape.bezierCurveTo(0, length, -width * 0.04, length, -width * 0.15, length * 0.92);
  shape.bezierCurveTo(-width, length * 0.62, -width * 0.55, length * 0.18, 0, 0.004);
  return shape;
}

function curvedPetal(length: number, width: number, curl: number) {
  const geometry = new ExtrudeGeometry(petalShape(length, width), {
    depth: 0.0035,
    bevelEnabled: true,
    bevelThickness: 0.0015,
    bevelSize: 0.0015,
    bevelSegments: 1,
    curveSegments: 5,
  });
  const pos = geometry.attributes.position;
  for (let i = 0; i < pos.count; i += 1) {
    const y = pos.getY(i);
    const t = Math.min(1, Math.max(0, y / length));
    pos.setZ(i, pos.getZ(i) - 0.0018 + Math.sin(t * Math.PI) * curl);
    pos.setX(i, pos.getX(i) * (1 - t * 0.08));
  }
  geometry.computeVertexNormals();
  return geometry;
}

function placePetal(length: number, width: number, curl: number, angle: number, radius: number, lift: number, tilt: number) {
  const mesh = new Mesh(curvedPetal(length, width, curl));
  mesh.position.set(Math.cos(angle) * radius, lift, Math.sin(angle) * radius);
  mesh.rotation.set(tilt, -angle, 0);
  return mesh;
}

function ring(count: number, length: number, width: number, curl: number, radius: number, lift: number, tilt: number) {
  return Array.from({ length: count }, (_, index) => {
    const angle = (index / count) * Math.PI * 2;
    return placePetal(length, width, curl, angle, radius, lift, tilt);
  });
}

function diskMesh(radius: number, y: number) {
  const shape = new Shape();
  shape.absarc(0, 0, radius, 0, Math.PI * 2, false);
  const geometry = new ExtrudeGeometry(shape, { depth: 0.014, bevelEnabled: true, bevelThickness: 0.004, bevelSize: 0.003, bevelSegments: 1, curveSegments: 12 });
  const mesh = new Mesh(geometry);
  mesh.rotation.x = -Math.PI / 2;
  mesh.position.y = y;
  return mesh;
}

function rose(): BloomParts {
  return {
    petals: merge([
      ...ring(5, 0.042, 0.022, 0.02, 0.008, 0.02, 0.35),
      ...ring(6, 0.055, 0.026, 0.024, 0.014, 0.012, 0.62),
      ...ring(7, 0.07, 0.032, 0.03, 0.022, 0.004, 0.95),
      ...ring(8, 0.082, 0.036, 0.034, 0.03, 0, 1.2),
    ]),
  };
}

function tulip(): BloomParts {
  return {
    petals: merge(ring(6, 0.125, 0.038, -0.028, 0.016, 0.015, 0.22)),
    accent: { geometry: merge([placePetal(0.028, 0.01, 0.004, 0, 0, 0.045, 0.1)]), color: '#d4b15a' },
  };
}

function sunflower(): BloomParts {
  return {
    petals: merge(ring(18, 0.11, 0.02, 0.01, 0.055, 0.008, 1.35)),
    accent: { geometry: merge([diskMesh(0.048, 0.02)]), color: '#6b3a16' },
  };
}

function daisy(): BloomParts {
  return {
    petals: merge(ring(10, 0.075, 0.016, 0.006, 0.03, 0.008, 1.4)),
    accent: { geometry: merge([placePetal(0.022, 0.022, 0, 0, 0, 0.012, 1.2)]), color: '#e0a106' },
  };
}

function hydrangea(): BloomParts {
  const florets: Mesh[] = [];
  const offsets: Array<[number, number, number]> = [
    [0, 0.02, 0],
    [0.05, 0.012, 0.018],
    [-0.046, 0.014, 0.02],
    [0.02, 0.016, -0.048],
    [-0.022, 0.02, -0.04],
    [0.034, 0.03, 0.03],
    [-0.01, 0.034, 0.01],
  ];
  for (const [x, y, z] of offsets) {
    for (let index = 0; index < 4; index += 1) {
      const angle = (index / 4) * Math.PI * 2 + x;
      const petal = placePetal(0.03, 0.02, 0.008, angle, 0.014, y, 0.95);
      petal.position.x += x;
      petal.position.z += z;
      florets.push(petal);
    }
  }
  return { petals: merge(florets) };
}

function alstroemeria(): BloomParts {
  const outer = [0, 2, 4].map((index) => placePetal(0.12, 0.036, 0.022, (index / 6) * Math.PI * 2, 0.02, 0.008, 0.8));
  const inner = [1, 3, 5].map((index) => placePetal(0.095, 0.02, 0.016, (index / 6) * Math.PI * 2, 0.014, 0.02, 0.55));
  const spots = [1, 3, 5].flatMap((index) => {
    const angle = (index / 6) * Math.PI * 2;
    return [0, 1, 2].map((spot) => placePetal(0.01, 0.007, 0.001, angle, 0.028 + spot * 0.012, 0.03, 1.05));
  });
  return {
    petals: merge([...outer, ...inner]),
    accent: { geometry: merge(spots), color: ALSTROEMERIA_SPOT },
  };
}

function lily(): BloomParts {
  return {
    petals: merge(ring(6, 0.12, 0.028, 0.03, 0.03, 0.02, 0.7)),
    accent: {
      geometry: merge([0, 1, 2].map((index) => placePetal(0.02, 0.006, 0, index * 2.1, 0.01, 0.05, 0.2))),
      color: '#e0a106',
    },
  };
}

export function sharedLeafGeometry() {
  if (!leafCache) {
    leafCache = merge([
      placePetal(0.09, 0.028, 0.02, 0.4, 0.01, 0, 0.6),
      placePetal(0.07, 0.022, 0.016, 2.4, 0.012, 0.01, 0.7),
    ]);
  }
  return leafCache;
}

function build(kind: FlowerKind): BloomParts {
  switch (kind) {
    case 'rose':
      return rose();
    case 'tulip':
      return tulip();
    case 'sunflower':
      return sunflower();
    case 'daisy':
      return daisy();
    case 'lily':
      return lily();
    case 'hydrangea':
      return hydrangea();
    case 'alstroemeria':
      return alstroemeria();
    case 'foliage':
      return { petals: sharedLeafGeometry() };
    default:
      return { petals: merge(ring(6, 0.08, 0.028, 0.014, 0.02, 0.01, 0.9)) };
  }
}

export function bloomGeometry(kind: FlowerKind) {
  const cached = cache.get(kind);
  if (cached) return cached;
  const parts = build(kind);
  cache.set(kind, parts);
  return parts;
}
