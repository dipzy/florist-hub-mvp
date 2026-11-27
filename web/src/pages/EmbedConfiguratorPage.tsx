import { useEffect, useMemo, useState } from 'react';
import { usePageText } from '../lib/usePageText';
import { useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Flower } from '../api/types';
import { ArrangementConfigurator } from '../configurator/ArrangementConfigurator';
import { DEMO_BASKET, DEMO_FLOWERS } from '../configurator/demoBouquet';
import { resolveBaseType, type ArrangementBaseId, type ArrangementShapeId, type StemInput } from '../configurator/layout';
import { isFlowerExhausted } from '../lib/capacity';
import { applyStoreThemeFromColors } from '../lib/storeTheme';

function readShape(value: string | null): ArrangementShapeId {
  const parsed = Number(value);
  return parsed === 1 || parsed === 2 ? parsed : 0;
}

function cssColor(value: string | null) {
  if (!value) return null;
  const color = value.trim();
  return /^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/.test(color) ? color : null;
}

function readSeed(value: string | null) {
  if (!value) return 17483021;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : 17483021;
}

function basketFromQuery(flowers: Flower[], raw: string | null): StemInput[] {
  if (!raw) return [];
  const items: StemInput[] = [];
  for (const part of raw.split('|')) {
    const separator = part.lastIndexOf(':');
    if (separator <= 0) continue;
    const name = part.slice(0, separator).trim();
    const quantity = Number(part.slice(separator + 1));
    if (!name || !Number.isFinite(quantity) || quantity <= 0) continue;
    const flower = flowers.find((candidate) => candidate.name.toLowerCase() === name.toLowerCase());
    items.push({ name: flower?.name ?? name, quantity: Math.floor(quantity), color: flower?.color });
  }
  return items;
}

/**
 * Vista sem a navegação da loja, para a app móvel abrir numa WebView.
 * A app KMP abre esta rota numa WebView e passa o slug e as cores da loja.
 */
export function EmbedConfiguratorPage() {
  const { pt } = usePageText('pages/EmbedConfiguratorPage');
  const [params] = useSearchParams();
  const demo = params.get('demo') === '1';
  const [flowers, setFlowers] = useState<Flower[]>(demo ? DEMO_FLOWERS : []);
  const [basket, setBasket] = useState<StemInput[]>(() => (demo ? DEMO_BASKET : []));
  const [shape, setShape] = useState<ArrangementShapeId>(readShape(params.get('shape')));
  const [withVase, setWithVase] = useState(params.get('vase') !== '0');
  const [seed, setSeed] = useState(readSeed(params.get('seed')));
  const [error, setError] = useState<string | null>(null);

  const baseType = useMemo(() => resolveBaseType(shape, shape === 0 ? false : withVase), [shape, withVase]);
  const themed = Boolean(cssColor(params.get('primary')) || cssColor(params.get('accent')));

  useEffect(() => {
    applyStoreThemeFromColors(cssColor(params.get('primary')), cssColor(params.get('accent')));
  }, [params]);

  useEffect(() => {
    if (demo) return;
    api
      .flowers()
      .then((catalog) => {
        setFlowers(catalog);
        const fromQuery = basketFromQuery(catalog, params.get('items'));
        if (fromQuery.length > 0) setBasket(fromQuery);
      })
      .catch(() => setError(pt("Não foi possível carregar o catálogo desta loja.")));
  }, [demo, params]);

  useEffect(() => {
    const payload = {
      type: 'floristhub:arrangement',
      seed,
      shape,
      withVase,
      baseType,
      items: basket,
    };
    window.parent.postMessage(payload, '*');
  }, [baseType, basket, seed, shape, withVase]);

  function addFlower(flower: Flower) {
    setBasket((current) => {
      const existing = current.find((item) => item.name === flower.name);
      if (existing) {
        return current.map((item) => (item.name === flower.name ? { ...item, quantity: item.quantity + 1 } : item));
      }
      return [...current, { name: flower.name, quantity: 1, color: flower.color }];
    });
  }

  function changeQuantity(name: string, delta: number) {
    setBasket((current) =>
      current
        .map((item) => (item.name === name ? { ...item, quantity: item.quantity + delta } : item))
        .filter((item) => item.quantity > 0),
    );
  }

  return (
    <main className={themed ? 'embed-configurator embed-configurator-themed' : 'embed-configurator'}>
      <h1>Configurador de ramo</h1>
      <p className="muted">Escolha as flores da loja, o formato e se leva vaso. A variação usa uma semente fixa.</p>
      {error && <p className="error-banner">{error}</p>}
      <ArrangementConfigurator
        items={basket}
        shape={shape}
        baseType={baseType as ArrangementBaseId}
        seed={seed}
        onShapeChange={setShape}
        onBaseTypeChange={(next) => setWithVase(next === 0)}
        onSeedChange={setSeed}
      />
      <h2>Flores</h2>
      <ul className="list">
        {flowers.map((flower) => {
          const line = basket.find((item) => item.name === flower.name);
          const exhausted = isFlowerExhausted(flower);
          return (
            <li key={flower.id} className="configurator-flower-row">
              <span>
                {flower.name}
                {flower.color ? <span className="muted"> · {flower.color}</span> : null}
                {exhausted ? <span className="muted"> · esgotada</span> : null}
              </span>
              {line ? (
                <span className="row">
                  <button type="button" className="secondary" onClick={() => changeQuantity(flower.name, -1)} aria-label={`Menos ${flower.name}`}>
                    −
                  </button>
                  <span>{line.quantity}</span>
                  <button type="button" onClick={() => changeQuantity(flower.name, 1)} aria-label={`Mais ${flower.name}`} disabled={exhausted}>
                    +
                  </button>
                </span>
              ) : (
                <button type="button" onClick={() => addFlower(flower)} disabled={exhausted}>
                  Adicionar
                </button>
              )}
            </li>
          );
        })}
      </ul>
    </main>
  );
}
