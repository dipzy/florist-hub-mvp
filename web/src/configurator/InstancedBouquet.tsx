import { useLayoutEffect, useMemo, useRef } from 'react';
import { useThree } from '@react-three/fiber';
import {
  Color,
  CylinderGeometry,
  Euler,
  InstancedMesh,
  Matrix4,
  Mesh,
  Quaternion,
  Vector3,
  type BufferGeometry,
} from 'three';
import { bloomGeometry, sharedLeafGeometry } from './bloomGeometry';
import { FLOWER_KINDS, type FlowerKind } from './flowerKind';
import type { ArrangementLayout, PlacedStem } from './layout';

const stemGeometry = new CylinderGeometry(0.011, 0.018, 1, 8);
const leafGeometry = sharedLeafGeometry();
const tuftGeometry = sharedLeafGeometry();
const up = new Vector3(0, 1, 0);
const scratch = new Matrix4();
const position = new Vector3();
const quaternion = new Quaternion();
const scale = new Vector3();
const tint = new Color();
const euler = new Euler();

function stemMatrix(stem: PlacedStem, target: Matrix4) {
  const dx = stem.head.x - stem.base.x;
  const dy = stem.head.y - stem.base.y;
  const dz = stem.head.z - stem.base.z;
  const length = Math.max(0.05, Math.hypot(dx, dy, dz));
  position.set((stem.base.x + stem.head.x) / 2, (stem.base.y + stem.head.y) / 2, (stem.base.z + stem.head.z) / 2);
  quaternion.setFromUnitVectors(up, new Vector3(dx, dy, dz).normalize());
  return target.compose(position, quaternion, scale.set(1, length, 1));
}

function headMatrix(stem: PlacedStem, target: Matrix4) {
  position.set(stem.head.x, stem.head.y, stem.head.z);
  quaternion.set(stem.quaternion.x, stem.quaternion.y, stem.quaternion.z, stem.quaternion.w);
  return target.compose(position, quaternion, scale.set(stem.scale * 1.35, stem.scale * 1.35, stem.scale * 1.35));
}

function Instances({
  geometry,
  count,
  color,
  roughness = 0.55,
  place,
  paint,
}: {
  geometry: BufferGeometry;
  count: number;
  color?: string;
  roughness?: number;
  place: (index: number, target: Matrix4) => Matrix4;
  paint?: (index: number) => string;
}) {
  const ref = useRef<InstancedMesh>(null);
  const invalidate = useThree((state) => state.invalidate);

  useLayoutEffect(() => {
    const mesh = ref.current;
    if (!mesh || count === 0) return;
    for (let index = 0; index < count; index += 1) {
      mesh.setMatrixAt(index, place(index, scratch));
      if (paint) mesh.setColorAt(index, tint.set(paint(index)));
    }
    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
    mesh.computeBoundingSphere();
    invalidate();
  }, [count, invalidate, paint, place]);

  if (count === 0) return null;
  return (
    <instancedMesh ref={ref} args={[geometry, undefined, count]} frustumCulled={false}>
      <meshPhysicalMaterial color={color ?? '#ffffff'} roughness={roughness} metalness={0} sheen={0.25} sheenRoughness={0.6} />
    </instancedMesh>
  );
}

function BloomMesh({
  geometry,
  stem,
  color,
}: {
  geometry: BufferGeometry;
  stem: PlacedStem;
  color: string;
}) {
  const ref = useRef<Mesh>(null);
  const invalidate = useThree((state) => state.invalidate);
  useLayoutEffect(() => {
    const mesh = ref.current;
    if (!mesh) return;
    mesh.matrixAutoUpdate = false;
    mesh.matrix.copy(headMatrix(stem, scratch));
    invalidate();
  }, [invalidate, stem]);
  return (
    <mesh ref={ref} geometry={geometry} frustumCulled={false}>
      <meshPhysicalMaterial color={color} roughness={0.5} metalness={0} sheen={0.18} sheenRoughness={0.45} sheenColor={color} />
    </mesh>
  );
}

function KindBlooms({ kind, stems }: { kind: FlowerKind; stems: PlacedStem[] }) {
  const parts = useMemo(() => bloomGeometry(kind), [kind]);

  return (
    <>
      {stems.map((stem, index) => (
        <BloomMesh key={`${stem.name}-${index}`} geometry={parts.petals} stem={stem} color={stem.color} />
      ))}
      {parts.accent &&
        stems.map((stem, index) => (
          <BloomMesh
            key={`${stem.name}-accent-${index}`}
            geometry={parts.accent!.geometry}
            stem={stem}
            color={parts.accent!.color}
          />
        ))}
    </>
  );
}

/**
 * Caules, folhas e flores: um InstancedMesh por peça partilhada.
 * O número de hastes já vem limitado por MAX_STEMS.
 */
export function InstancedBouquet({ layout, stems }: { layout: ArrangementLayout; stems: PlacedStem[] }) {
  const byKind = useMemo(() => {
    const groups = new Map<FlowerKind, PlacedStem[]>();
    for (const stem of stems) {
      const list = groups.get(stem.kind) ?? [];
      list.push(stem);
      groups.set(stem.kind, list);
    }
    return groups;
  }, [stems]);

  const leafy = useMemo(() => stems.filter((stem) => stem.kind !== 'foliage'), [stems]);
  const placeStem = useMemo(() => (index: number, target: Matrix4) => stemMatrix(stems[index], target), [stems]);
  const placeLeafAt = useMemo(
    () => (along: number, flip: number) => (index: number, target: Matrix4) => {
      const stem = leafy[index];
      position.set(
        stem.base.x + (stem.head.x - stem.base.x) * along + Math.cos(stem.leaf + flip) * 0.045,
        stem.base.y + (stem.head.y - stem.base.y) * along,
        stem.base.z + (stem.head.z - stem.base.z) * along + Math.sin(stem.leaf + flip) * 0.045,
      );
      euler.set(0.45, stem.leaf + flip, 0.15);
      quaternion.setFromEuler(euler);
      return target.compose(position, quaternion, scale.set(0.9, 0.9, 0.9));
    },
    [leafy],
  );
  const placeLeaf = useMemo(() => placeLeafAt(0.34, 0), [placeLeafAt]);
  const placeUpperLeaf = useMemo(() => placeLeafAt(0.58, Math.PI), [placeLeafAt]);
  const placeTuft = useMemo(
    () => (index: number, target: Matrix4) => {
      const tuft = layout.foliage[index];
      position.set(tuft.position.x, tuft.position.y, tuft.position.z);
      euler.set(0.5, tuft.rotationY, 0.2);
      quaternion.setFromEuler(euler);
      return target.compose(position, quaternion, scale.set(tuft.scale, tuft.scale, tuft.scale));
    },
    [layout.foliage],
  );

  return (
    <group>
      <Instances geometry={stemGeometry} count={stems.length} color="#2a5c34" roughness={0.72} place={placeStem} />
      <Instances geometry={leafGeometry} count={leafy.length} color="#3f7a48" roughness={0.55} place={placeLeaf} />
      <Instances geometry={leafGeometry} count={leafy.length} color="#4a8a4e" roughness={0.55} place={placeUpperLeaf} />
      <Instances geometry={tuftGeometry} count={layout.foliage.length} color="#4d8a52" roughness={0.58} place={placeTuft} />
      {FLOWER_KINDS.map((kind) => {
        const group = byKind.get(kind);
        if (!group || group.length === 0) return null;
        return <KindBlooms key={kind} kind={kind} stems={group} />;
      })}
    </group>
  );
}
