import { Component, Suspense, useMemo, type ReactNode } from 'react';
import { useGLTF } from '@react-three/drei';
import { MeshoptDecoder } from 'three/addons/libs/meshopt_decoder.module.js';

useGLTF.setDecoderPath('/draco/');
import { DoubleSide, Quaternion } from 'three';
import { shadeHex } from './colors';
import type { FlowerKind } from './flowerKind';
import type { PlacedStem } from './layout';

export type FlowerModelUrls = Partial<Record<FlowerKind, string>>;

function Rose({ color }: { color: string }) {
  return (
    <group>
      {[0, 1, 2, 3, 4].map((layer) => (
        <mesh
          key={layer}
          position={[0, layer * 0.015, 0]}
          rotation={[0.2, layer * 0.95, 0.1]}
          scale={[0.075 + layer * 0.014, 0.032, 0.058 + layer * 0.008]}
        >
          <sphereGeometry args={[1, 7, 5]} />
          <meshStandardMaterial color={layer < 2 ? shadeHex(color, -16) : color} roughness={0.52} />
        </mesh>
      ))}
    </group>
  );
}

function Tulip({ color }: { color: string }) {
  return (
    <group>
      {[0, 1, 2, 3, 4].map((index) => {
        const angle = (index / 5) * Math.PI * 2;
        return (
          <mesh key={index} position={[Math.cos(angle) * 0.028, 0.04, Math.sin(angle) * 0.028]} rotation={[0.45, angle, 0]}>
            <coneGeometry args={[0.04, 0.15, 5]} />
            <meshStandardMaterial color={color} roughness={0.48} />
          </mesh>
        );
      })}
      <mesh position={[0, 0.02, 0]}>
        <sphereGeometry args={[0.03, 6, 5]} />
        <meshStandardMaterial color={shadeHex(color, -20)} />
      </mesh>
    </group>
  );
}

function Sunflower({ color }: { color: string }) {
  return (
    <group>
      {Array.from({ length: 12 }, (_, index) => {
        const angle = (index / 12) * Math.PI * 2;
        return (
          <mesh key={index} position={[Math.cos(angle) * 0.09, 0.012, Math.sin(angle) * 0.09]} rotation={[Math.PI / 2.2, 0, angle]} scale={[0.04, 0.085, 0.018]}>
            <sphereGeometry args={[1, 6, 4]} />
            <meshStandardMaterial color={color} roughness={0.62} />
          </mesh>
        );
      })}
      <mesh>
        <sphereGeometry args={[0.055, 8, 6]} />
        <meshStandardMaterial color="#6b3e1e" roughness={0.9} />
      </mesh>
    </group>
  );
}

function Daisy({ color }: { color: string }) {
  return (
    <group>
      {Array.from({ length: 10 }, (_, index) => {
        const angle = (index / 10) * Math.PI * 2;
        return (
          <mesh key={index} position={[Math.cos(angle) * 0.07, 0.01, Math.sin(angle) * 0.07]} rotation={[Math.PI / 2.15, 0, angle]} scale={[0.028, 0.07, 0.012]}>
            <sphereGeometry args={[1, 6, 4]} />
            <meshStandardMaterial color={color} roughness={0.5} />
          </mesh>
        );
      })}
      <mesh position={[0, 0.015, 0]}>
        <sphereGeometry args={[0.032, 8, 6]} />
        <meshStandardMaterial color="#e0a106" roughness={0.7} />
      </mesh>
    </group>
  );
}

function Lily({ color }: { color: string }) {
  return (
    <group>
      {[0, 1, 2, 3, 4, 5].map((index) => {
        const angle = (index / 6) * Math.PI * 2;
        return (
          <mesh key={index} position={[Math.cos(angle) * 0.045, 0.03, Math.sin(angle) * 0.045]} rotation={[0.75, angle, 0]} scale={[0.03, 0.11, 0.016]}>
            <sphereGeometry args={[1, 6, 4]} />
            <meshStandardMaterial color={color} roughness={0.42} />
          </mesh>
        );
      })}
      {[0, 1, 2].map((index) => (
        <mesh key={`stamen-${index}`} position={[Math.cos(index * 2.1) * 0.018, 0.07, Math.sin(index * 2.1) * 0.018]}>
          <sphereGeometry args={[0.012, 5, 4]} />
          <meshStandardMaterial color="#e0a106" />
        </mesh>
      ))}
    </group>
  );
}

function GenericBloom({ color }: { color: string }) {
  return (
    <group>
      {[0, 1, 2, 3, 4].map((index) => {
        const angle = (index / 5) * Math.PI * 2;
        return (
          <mesh key={index} position={[Math.cos(angle) * 0.05, 0.02, Math.sin(angle) * 0.05]} scale={[0.04, 0.025, 0.055]}>
            <sphereGeometry args={[1, 6, 5]} />
            <meshStandardMaterial color={color} roughness={0.5} />
          </mesh>
        );
      })}
      <mesh>
        <sphereGeometry args={[0.028, 7, 5]} />
        <meshStandardMaterial color={shadeHex(color, -25)} />
      </mesh>
    </group>
  );
}

function FoliageSprig({ color }: { color: string }) {
  return (
    <group>
      {[0, 1, 2].map((index) => (
        <mesh key={index} position={[index * 0.02 - 0.02, 0.04 + index * 0.03, 0]} rotation={[0.4, index, 0.6]} scale={[0.045, 0.016, 0.09]}>
          <sphereGeometry args={[1, 6, 4]} />
          <meshStandardMaterial color={index === 1 ? color : shadeHex(color, -12)} roughness={0.7} side={DoubleSide} />
        </mesh>
      ))}
    </group>
  );
}

function ProceduralBloom({ kind, color }: { kind: FlowerKind; color: string }) {
  switch (kind) {
    case 'rose':
      return <Rose color={color} />;
    case 'tulip':
      return <Tulip color={color} />;
    case 'sunflower':
      return <Sunflower color={color} />;
    case 'daisy':
      return <Daisy color={color} />;
    case 'lily':
      return <Lily color={color} />;
    case 'foliage':
      return <FoliageSprig color={color} />;
    default:
      return <GenericBloom color={color} />;
  }
}

function GlbBloom({ url }: { url: string }) {
  const { scene } = useGLTF(url, '/draco/', false, (loader) => {
    (loader as unknown as { setMeshoptDecoder: (decoder: unknown) => void }).setMeshoptDecoder(MeshoptDecoder);
  });
  const clone = useMemo(() => scene.clone(true), [scene]);
  return <primitive object={clone} scale={0.22} />;
}

class GlbBoundary extends Component<{ fallback: ReactNode; children: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  render() {
    return this.state.failed ? this.props.fallback : this.props.children;
  }
}

export function PlacedFlower({ stem, modelUrl }: { stem: PlacedStem; modelUrl?: string }) {
  const quaternion = useMemo(
    () => new Quaternion(stem.quaternion.x, stem.quaternion.y, stem.quaternion.z, stem.quaternion.w),
    [stem.quaternion.x, stem.quaternion.y, stem.quaternion.z, stem.quaternion.w],
  );
  const length = Math.hypot(stem.head.x - stem.base.x, stem.head.y - stem.base.y, stem.head.z - stem.base.z);
  const mid: [number, number, number] = [
    (stem.base.x + stem.head.x) / 2,
    (stem.base.y + stem.head.y) / 2,
    (stem.base.z + stem.head.z) / 2,
  ];
  const leafT = 0.38;
  const leaf: [number, number, number] = [
    stem.base.x + (stem.head.x - stem.base.x) * leafT + Math.cos(stem.leaf) * 0.05,
    stem.base.y + (stem.head.y - stem.base.y) * leafT,
    stem.base.z + (stem.head.z - stem.base.z) * leafT + Math.sin(stem.leaf) * 0.05,
  ];
  const bloom = <ProceduralBloom kind={stem.kind} color={stem.color} />;

  return (
    <group>
      <mesh position={mid} quaternion={quaternion}>
        <cylinderGeometry args={[0.008, 0.014, Math.max(0.05, length), 5]} />
        <meshStandardMaterial color="#3c6b45" roughness={0.75} />
      </mesh>
      {stem.kind !== 'foliage' && (
        <mesh position={leaf} scale={[0.05, 0.012, 0.08]} rotation={[0.4, stem.leaf, 0.2]}>
          <sphereGeometry args={[1, 6, 4]} />
          <meshStandardMaterial color="#4f8a58" roughness={0.7} />
        </mesh>
      )}
      <group position={[stem.head.x, stem.head.y, stem.head.z]} quaternion={quaternion} scale={stem.scale}>
        {modelUrl ? (
          <GlbBoundary fallback={bloom}>
            <Suspense fallback={bloom}>
              <GlbBloom url={modelUrl} />
            </Suspense>
          </GlbBoundary>
        ) : (
          bloom
        )}
      </group>
    </group>
  );
}
