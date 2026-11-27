import { useEffect, useRef, type MutableRefObject } from 'react';
import { usePageText } from '../lib/usePageText';
import type { ArrangementLayout, PlacedStem } from './layout';

function project(stem: { x: number; y: number; z: number }, width: number, height: number) {
  const scale = Math.min(width, height) * 0.42;
  return {
    x: width / 2 + stem.x * scale + stem.z * scale * 0.18,
    y: height * 0.78 - stem.y * scale,
    depth: stem.z,
  };
}

function paintHead(ctx: CanvasRenderingContext2D, x: number, y: number, stem: PlacedStem) {
  const radius = 10 * stem.scale;
  ctx.save();
  ctx.translate(x, y);
  ctx.fillStyle = stem.color;
  const petals = stem.kind === 'sunflower' ? 10 : stem.kind === 'daisy' ? 8 : stem.kind === 'lily' ? 6 : 5;
  if (stem.kind === 'foliage') {
    ctx.fillStyle = '#4f8a58';
    ctx.beginPath();
    ctx.ellipse(0, 0, radius * 1.4, radius * 0.7, 0.6, 0, Math.PI * 2);
    ctx.fill();
  } else {
    for (let index = 0; index < petals; index += 1) {
      const angle = (index / petals) * Math.PI * 2;
      ctx.beginPath();
      ctx.ellipse(Math.cos(angle) * radius * 0.7, Math.sin(angle) * radius * 0.7, radius * 0.55, radius * 0.85, angle, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.fillStyle = stem.kind === 'sunflower' ? '#6b3e1e' : stem.kind === 'daisy' || stem.kind === 'lily' ? '#e0a106' : stem.color;
    ctx.beginPath();
    ctx.arc(0, 0, radius * 0.38, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

function paint(ctx: CanvasRenderingContext2D, width: number, height: number, layout: ArrangementLayout) {
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = '#f6f1e7';
  ctx.fillRect(0, 0, width, height);

  const base = project({ x: 0, y: 0.16, z: 0 }, width, height);
  if (layout.baseType === 0) {
    ctx.fillStyle = '#d7e7ee';
    ctx.beginPath();
    ctx.moveTo(base.x - 28, base.y + 36);
    ctx.lineTo(base.x + 28, base.y + 36);
    ctx.lineTo(base.x + 22, base.y - 8);
    ctx.lineTo(base.x - 22, base.y - 8);
    ctx.fill();
  } else if (layout.baseType === 1) {
    ctx.fillStyle = '#6d8f62';
    ctx.beginPath();
    ctx.ellipse(base.x, base.y + 20, 36, 12, 0, 0, Math.PI * 2);
    ctx.fill();
  } else {
    ctx.fillStyle = '#e7d3ae';
    ctx.beginPath();
    ctx.moveTo(base.x - 34, base.y + 28);
    ctx.lineTo(base.x + 34, base.y + 28);
    ctx.lineTo(base.x + 16, base.y - 16);
    ctx.lineTo(base.x - 16, base.y - 16);
    ctx.fill();
  }

  const ordered = [...layout.stems].sort(
    (a, b) => project(a.head, width, height).depth - project(b.head, width, height).depth,
  );
  for (const stem of ordered) {
    const from = project(stem.base, width, height);
    const to = project(stem.head, width, height);
    ctx.strokeStyle = '#3c6b45';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(from.x, from.y);
    ctx.lineTo(to.x, to.y);
    ctx.stroke();
    paintHead(ctx, to.x, to.y, stem);
  }
}

export function ArrangementFallback2D({
  layout,
  captureRef,
}: {
  layout: ArrangementLayout;
  captureRef: MutableRefObject<(() => Promise<Blob | null>) | null>;
}) {
  const { pt } = usePageText('configurator/ArrangementFallback2D');
  const wrapRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    const wrap = wrapRef.current;
    if (!canvas || !wrap) return;

    const draw = () => {
      const width = Math.max(1, wrap.clientWidth);
      const height = Math.max(1, wrap.clientHeight);
      const dpr = Math.min(2, window.devicePixelRatio || 1);
      canvas.width = Math.floor(width * dpr);
      canvas.height = Math.floor(height * dpr);
      const ctx = canvas.getContext('2d');
      if (!ctx) return;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      paint(ctx, width, height, layout);
    };

    draw();
    const observer = new ResizeObserver(draw);
    observer.observe(wrap);
    return () => observer.disconnect();
  }, [layout]);

  useEffect(() => {
    captureRef.current = () =>
      new Promise((resolve) => {
        canvasRef.current?.toBlob((blob) => resolve(blob), 'image/png');
      });
    return () => {
      captureRef.current = null;
    };
  }, [captureRef, layout]);

  return (
    <div ref={wrapRef} className="configurator-stage configurator-fallback">
      <canvas ref={canvasRef} aria-label={pt("Pré-visualização 2D do arranjo")} />
      <p className="configurator-hint">Pré-visualização simplificada, sem WebGL.</p>
    </div>
  );
}
