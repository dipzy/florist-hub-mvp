import { useEffect, useMemo, type MutableRefObject } from 'react';
import { Environment, OrbitControls } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import { DoubleSide, LatheGeometry, Vector2 } from 'three';
import { InstancedBouquet } from './InstancedBouquet';
import { PlacedFlower, type FlowerModelUrls } from './FlowerMesh';
import type { ArrangementLayout } from './layout';
import { useStagePalette } from './useStagePalette';
import { prefersLowPower } from './webgl';

const vaseGeometry = new LatheGeometry(
  [
    new Vector2(0.045, 0),
    new Vector2(0.09, 0.03),
    new Vector2(0.11, 0.12),
    new Vector2(0.078, 0.22),
    new Vector2(0.105, 0.3),
    new Vector2(0.145, 0.34),
  ],
  28,
);

function CaptureBridge({ captureRef }: { captureRef: MutableRefObject<(() => Promise<Blob | null>) | null> }) {
  const gl = useThree((state) => state.gl);
  const scene = useThree((state) => state.scene);
  const camera = useThree((state) => state.camera);

  useEffect(() => {
    captureRef.current = () =>
      new Promise((resolve) => {
        gl.render(scene, camera);
        gl.domElement.toBlob((blob) => resolve(blob), 'image/png');
      });
    return () => {
      captureRef.current = null;
    };
  }, [camera, captureRef, gl, scene]);

  return null;
}

function Vase() {
  return (
    <mesh geometry={vaseGeometry} position={[0, 0, 0]} castShadow>
      <meshPhysicalMaterial
        color="#e7f2f6"
        roughness={0.08}
        metalness={0}
        transmission={0.72}
        thickness={0.35}
        ior={1.45}
        transparent
        opacity={0.92}
      />
    </mesh>
  );
}

function Foam({ y, radius }: { y: number; radius: number }) {
  return (
    <group position={[0, y, 0]}>
      <mesh castShadow>
        <cylinderGeometry args={[radius * 0.96, radius, 0.055, 28]} />
        <meshStandardMaterial color="#5c7a3e" roughness={0.96} />
      </mesh>
      <mesh position={[0, 0.03, 0]}>
        <cylinderGeometry args={[radius * 0.9, radius * 0.92, 0.012, 28]} />
        <meshStandardMaterial color="#6d8c4a" roughness={1} />
      </mesh>
    </group>
  );
}

function PaperWrap({ ribbon }: { ribbon: string }) {
  return (
    <group>
      <mesh position={[0, 0.16, 0]}>
        <cylinderGeometry args={[0.09, 0.1, 0.06, 20]} />
        <meshStandardMaterial color="#6d8a45" roughness={0.95} />
      </mesh>
      <mesh position={[0, 0.15, 0]} castShadow>
        <cylinderGeometry args={[0.18, 0.08, 0.34, 28, 1, true]} />
        <meshStandardMaterial color="#c4a574" side={DoubleSide} roughness={0.9} />
      </mesh>
      <mesh position={[0, 0.2, 0]} rotation={[0.04, 0.4, 0]}>
        <cylinderGeometry args={[0.2, 0.09, 0.18, 24, 1, true]} />
        <meshStandardMaterial color="#d7bc90" side={DoubleSide} roughness={0.9} />
      </mesh>
      <mesh position={[0, 0.12, 0.02]} rotation={[Math.PI / 2, 0, 0]}>
        <torusGeometry args={[0.07, 0.01, 8, 20]} />
        <meshStandardMaterial color={ribbon} roughness={0.4} />
      </mesh>
      <group position={[0.02, 0.2, 0.1]}>
        <mesh rotation={[0.2, 0.2, 0.5]}>
          <torusGeometry args={[0.034, 0.008, 8, 16, Math.PI * 1.3]} />
          <meshStandardMaterial color={ribbon} roughness={0.35} />
        </mesh>
        <mesh rotation={[0.2, -0.2, -0.5]}>
          <torusGeometry args={[0.034, 0.008, 8, 16, Math.PI * 1.3]} />
          <meshStandardMaterial color={ribbon} roughness={0.35} />
        </mesh>
      </group>
    </group>
  );
}

function FitCamera({ layout }: { layout: ArrangementLayout }) {
  const camera = useThree((state) => state.camera);
  const controls = useThree((state) => state.controls) as {
    target: { set: (x: number, y: number, z: number) => void };
    update: () => void;
  } | null;
  const invalidate = useThree((state) => state.invalidate);
  const targetY = Math.max(0.34, layout.height * 0.48);
  const distance = Math.min(3.8, 1.15 + layout.height * 1.45 + layout.radius * 0.45);

  useEffect(() => {
    camera.position.set(distance * 0.08, targetY + distance * 0.22, distance);
    camera.lookAt(0, targetY, 0);
    controls?.target.set(0, targetY, 0);
    controls?.update();
    invalidate();
  }, [camera, controls, distance, invalidate, targetY]);

  return null;
}

function InvalidateOnLayout({ layout }: { layout: ArrangementLayout }) {
  const invalidate = useThree((state) => state.invalidate);
  useEffect(() => {
    invalidate();
  }, [invalidate, layout]);
  return null;
}

export function ArrangementScene({
  layout,
  modelUrls,
  captureRef,
}: {
  layout: ArrangementLayout;
  modelUrls?: FlowerModelUrls;
  captureRef: MutableRefObject<(() => Promise<Blob | null>) | null>;
}) {
  const palette = useStagePalette();
  const lowPower = prefersLowPower();
  const { procedural, custom } = useMemo(() => {
    const withModel = layout.stems.filter((stem) => modelUrls?.[stem.kind]);
    const plain = layout.stems.filter((stem) => !modelUrls?.[stem.kind]);
    return { procedural: plain, custom: withModel };
  }, [layout.stems, modelUrls]);

  return (
    <>
      <color attach="background" args={[palette.background]} />
      <ambientLight intensity={0.62} />
      <hemisphereLight args={[palette.ambient, palette.hemisphereGround, 0.42]} />
      <directionalLight position={[2.4, 5.2, 2.2]} intensity={0.9} />
      <directionalLight position={[-2.2, 2.4, -1.4]} intensity={0.28} color="#f3e6d4" />
      {!lowPower && <Environment files="/hdri/studio-cc0.hdr" environmentIntensity={0.55} />}

      {layout.baseType === 0 && <Vase />}
      {layout.baseType === 1 && <Foam y={0.04} radius={0.15} />}
      {layout.baseType === 0 && <Foam y={0.3} radius={0.07} />}
      {layout.baseType === 2 && <PaperWrap ribbon={palette.accent} />}

      <InstancedBouquet layout={layout} stems={procedural} />
      {custom.map((stem) => (
        <PlacedFlower key={`${stem.kind}-${stem.name}-${stem.head.x}`} stem={stem} modelUrl={modelUrls?.[stem.kind]} />
      ))}

      <OrbitControls
        makeDefault
        enablePan={false}
        minDistance={0.8}
        maxDistance={4.6}
        maxPolarAngle={Math.PI / 1.9}
        target={[0, Math.max(0.34, layout.height * 0.48), 0]}
      />
      <FitCamera layout={layout} />
      <CaptureBridge captureRef={captureRef} />
      <InvalidateOnLayout layout={layout} />
    </>
  );
}
