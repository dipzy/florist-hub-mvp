import { Canvas } from '@react-three/fiber';
import { ACESFilmicToneMapping } from 'three';
import { Component, Suspense, useImperativeHandle, useMemo, useRef, useState, type MutableRefObject, type ReactNode, type Ref } from 'react';
import { usePageText } from '../lib/usePageText';
import { ArrangementFallback2D } from './ArrangementFallback2D';
import { ArrangementScene } from './ArrangementScene';
import type { FlowerModelUrls } from './FlowerMesh';
import type { ArrangementLayout } from './layout';
import { shapeOptions } from './labels';
import { useTranslation } from 'react-i18next';
import { hasWebGL, prefersLowPower } from './webgl';
import {
  generateArrangement,
  resolveBaseType,
  shapeAllowsVase,
  type ArrangementBaseId,
  type ArrangementShapeId,
  type StemInput,
} from './layout';
import { randomSeed } from './prng';

export type ArrangementConfiguratorHandle = {
  capturePng: () => Promise<Blob | null>;
};

type Props = {
  ref?: Ref<ArrangementConfiguratorHandle>;
  items: StemInput[];
  shape: ArrangementShapeId;
  baseType: ArrangementBaseId;
  seed: number;
  readOnly?: boolean;
  modelUrls?: FlowerModelUrls;
  onShapeChange?: (shape: ArrangementShapeId) => void;
  onBaseTypeChange?: (baseType: ArrangementBaseId) => void;
  onSeedChange?: (seed: number) => void;
};

class StageErrorBoundary extends Component<
  { children: ReactNode; layout: ArrangementLayout; captureRef: MutableRefObject<(() => Promise<Blob | null>) | null> },
  { failed: boolean }
> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  render() {
    if (this.state.failed) {
      return <ArrangementFallback2D layout={this.props.layout} captureRef={this.props.captureRef} />;
    }
    return this.props.children;
  }
}

export function ArrangementConfigurator({
  ref,
  items,
  shape,
  baseType,
  seed,
  readOnly = false,
  modelUrls,
  onShapeChange,
  onBaseTypeChange,
  onSeedChange,
}: Props) {
  const { t } = useTranslation();
  const { pt } = usePageText('configurator/ArrangementConfigurator');
  const shapes = useMemo(() => shapeOptions(t), [t]);
  const captureRef = useRef<(() => Promise<Blob | null>) | null>(null);
  const [webgl] = useState(hasWebGL);
  const lowPower = useMemo(() => prefersLowPower(), []);
  const layout = useMemo(
    () => generateArrangement({ seed, shape, baseType, items }),
    [baseType, items, seed, shape],
  );

  useImperativeHandle(ref, () => ({
    capturePng: async () => (await captureRef.current?.()) ?? null,
  }));

  const vaseAllowed = shapeAllowsVase(shape);
  const withVase = vaseAllowed && baseType === 0;

  function setVase(next: boolean) {
    if (!vaseAllowed) return;
    onBaseTypeChange?.(resolveBaseType(shape, next));
  }

  function onShapePick(next: ArrangementShapeId) {
    onShapeChange?.(next);
    const keepVase = shapeAllowsVase(next) && withVase;
    onBaseTypeChange?.(resolveBaseType(next, keepVase));
  }

  return (
    <div className="configurator">
      <StageErrorBoundary layout={layout} captureRef={captureRef}>
        {webgl === false ? (
          <ArrangementFallback2D layout={layout} captureRef={captureRef} />
        ) : (
        <div className="configurator-stage" aria-label={pt("Pré-visualização 3D do arranjo")}>
          <Canvas
            camera={{ position: [0.15, 1.15, 2.2], fov: 38 }}
            dpr={[1, lowPower ? 1.25 : 1.5]}
            frameloop="demand"
            gl={{
              antialias: !lowPower,
              preserveDrawingBuffer: true,
              powerPreference: lowPower ? 'low-power' : 'default',
              alpha: false,
            }}
            onCreated={({ gl }) => {
              gl.toneMapping = ACESFilmicToneMapping;
              gl.toneMappingExposure = 1.05;
            }}
          >
            <Suspense fallback={null}>
              <ArrangementScene layout={layout} modelUrls={modelUrls} captureRef={captureRef} />
            </Suspense>
          </Canvas>
          {layout.stems.length === 0 && <p className="configurator-empty">Adicione flores do catálogo para ver o ramo.</p>}
          <p className="configurator-hint">Arraste para rodar. Use a roda do rato ou a pinça para aproximar.</p>
        </div>
        )}
      </StageErrorBoundary>

      {!readOnly && (
        <div className="configurator-controls">
          <div className="fh-chip-group" role="radiogroup" aria-label={t('configurator.format')}>
            <span className="fh-chip-group__label">{t('configurator.format')}</span>
            {shapes.map((option) => (
              <button
                key={option.id}
                type="button"
                role="radio"
                aria-checked={shape === option.id}
                className={shape === option.id ? 'fh-btn fh-chip is-active' : 'fh-btn fh-chip'}
                onClick={() => onShapePick(option.id)}
              >
                {option.label}
              </button>
            ))}
          </div>
          {vaseAllowed ? (
            <div className="fh-chip-group" role="radiogroup" aria-label={t('configurator.baseHeading')}>
              <span className="fh-chip-group__label">{t('configurator.baseHeading')}</span>
              <button
                type="button"
                role="radio"
                aria-checked={!withVase}
                className={!withVase ? 'fh-btn fh-chip is-active' : 'fh-btn fh-chip'}
                onClick={() => setVase(false)}
              >
                {t('configurator.wrapNoVase')}
              </button>
              <button
                type="button"
                role="radio"
                aria-checked={withVase}
                className={withVase ? 'fh-btn fh-chip is-active' : 'fh-btn fh-chip'}
                onClick={() => setVase(true)}
              >
                {t('configurator.withVase')}
              </button>
            </div>
          ) : (
            <p className="muted configurator-meta">{t('configurator.foamNoVase')}</p>
          )}
          <button type="button" className="fh-btn fh-btn--secondary" onClick={() => onSeedChange?.(randomSeed())}>
            {t('configurator.anotherVariation', { defaultValue: 'Gerar outra variação' })}
          </button>
        </div>
      )}
    </div>
  );
}
