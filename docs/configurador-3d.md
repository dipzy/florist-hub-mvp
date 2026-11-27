# Configurador 3D de ramos

O cliente compõe um arranjo com as flores do catálogo da loja. A web desenha um exemplo estilizado (não fotorrealista) e a florista vê a mesma variação no detalhe da encomenda.

## Onde aparece

- **Montar arranjo** (`/montar`), no fluxo de nova encomenda, antes do resumo e da entrega.
- **Detalhe da encomenda** do cliente (`/encomendas/:id`).
- **Painel da florista** (`/admin/encomendas`): miniatura, quando existir, e o botão «Ver arranjo 3D».
- **Vista embutida** (`/embed/configurador?tenant=<slug>`), sem a navegação da loja.

O configurador entra por `React.lazy`. Three.js, `@react-three/fiber` e `@react-three/drei` ficam num chunk separado do resto da aplicação.

## Opções

| Opção | Valores | Onde fica |
| --- | --- | --- |
| Formato | Esponja, Comprido, Curto | `Orders.ArrangementShape` |
| Base | Jarra, Esponja, Embrulho | `Orders.BaseType` (o valor `Wrapped = 2` foi acrescentado) |
| Variação | semente inteira | `Orders.ArrangementVariationSeed` |
| Pré-visualização | URL do PNG | `Orders.ArrangementPreviewUrl` |

«Com vaso» grava a base **Jarra**. Sem vaso, o formato Esponja grava **Esponja** (espuma) e os formatos Comprido ou Curto gravam **Embrulho** (papel). A migração é `20261110105641_AddArrangement3DOptions`.

O botão **Gerar outra variação** troca a semente. A mesma semente, o mesmo formato, a mesma base e as mesmas flores produzem a mesma montagem. O token `Arrangement3DPreviewToken` repete esses dados (`v1;order=…;seed=…;shape=…;base=…`) para quem já lia esse campo.

## Montagem

O gerador está em `web/src/configurator/layout.ts` e não depende do WebGL, para os testes poderem fixar o resultado.

- **Esponja:** cúpula (distribuição em anel, mais larga no meio).
- **Comprido:** espiral alta e estreita.
- **Curto:** espiral baixa.
- Caules, uma folha por haste e tufos de folhagem à volta da base.
- Com vaso: jarra. Sem vaso e em esponja: disco de espuma. Sem vaso nos outros formatos: papel de embrulho.

Há no máximo 18 hastes desenhadas. As quantidades pedidas na encomenda não são alteradas; o desenho só reduz a amostra quando o ramo seria pesado demais num portátil ou num telemóvel.

### Modelos

Feitos por código, com cor vinda do campo `color` da flor (nomes em português ou hexadecimal). Sem cor, usa-se uma pista pelo nome.

| Nome no catálogo | Modelo |
| --- | --- |
| rosa | rosa |
| túlipa | túlipa |
| girassol | girassol |
| margarida, gérbera | margarida |
| lírio, açucena, orquídea | lírio |
| eucalipto, folhagem, ruscus, gipsofila | ramo de folhas |
| qualquer outro | flor genérica |

Para trocar um modelo por um GLB mais tarde, passa-se `modelUrls` ao configurador, por exemplo `{ rose: '/modelos/rosa.glb' }`. O ficheiro tem de ser servido pela própria aplicação. Se o carregamento falhar, volta ao modelo de código. Este repositório não inclui modelos de terceiros.

## Pré-visualização PNG

Ao submeter a encomenda, o canvas exporta um PNG (`preserveDrawingBuffer`) e envia-o para `POST /api/files/upload`, o mesmo upload das fotos. O cliente autenticado também pode usar esse endpoint, para além da florista. Se o upload falhar, a encomenda segue na mesma: a florista reconstrói o arranjo pela semente.

## Desempenho e controlos

- No máximo 18 hastes desenhadas. Caules, folhas, tufos e flores procedurais usam `InstancedMesh` (uma geometria partilhada por tipo), em `InstancedBouquet.tsx`.
- As flores de código fundem as pétalas numa só geometria com poucos segmentos (`bloomGeometry.ts`).
- `dpr` limitado a 1,5 no computador e a 1,25 em ecrã estreito ou com ponteiro grosseiro. Nesse caso o antialiasing desliga-se e a GPU fica em `low-power`.
- `frameloop="demand"`. Sem sombras calculadas.
- `OrbitControls`: arrastar roda o ramo; a roda do rato ou a pinça aproximam. Não há deslocação lateral.
- Sem WebGL, ou com `?webgl=0`, ou se o canvas falhar, o mesmo gerador desenha-se em 2D (`ArrangementFallback2D`). O PNG da encomenda continua a sair desse desenho.

## Vista para a app móvel

O ecrã **Configurador 3D** da app KMP abre `/embed/configurador` com o slug e as cores da loja:

`{web}/embed/configurador?tenant=<slug>&primary=%23<hex>&accent=%23<hex>`

- Android: `WebView` (`BouquetWebView.android.kt`). O endereço por omissão é `http://10.0.2.2:5173` (`floristHub.webBaseUrl` / `WEB_BASE_URL`). O manifesto permite HTTP local (`usesCleartextTraffic`).
- iOS: `WKWebView` (`BouquetWebView.ios.kt`). O endereço por omissão é `http://127.0.0.1:5173`, ou `FLORIST_HUB_WEB_BASE_URL` no Info.plist.
- As cores `primary` e `accent` têm de ser hexadecimais (`#rgb`, `#rrggbb` ou `#rrggbbaa`). A página aplica-as a `--store-primary` e `--store-accent`.
- No modo de pré-visualização de capturas da app não se abre a WebView.

Parâmetros:

- `tenant` — slug da loja (o mesmo mecanismo do resto do site).
- `shape` — `0` Esponja, `1` Comprido, `2` Curto.
- `vase` — `1` com vaso, `0` sem vaso.
- `seed` — semente da variação.
- `items` — linhas `Nome:quantidade` separadas por `|`. Exemplo: `Rosa vermelha:3|Túlipa amarela:2`.
- `demo=1` — catálogo de exemplo, só para capturas e desenvolvimento. Não usar na app.
- `webgl=0` — força a pré-visualização 2D.
- `primary` e `accent` — cores da loja, em hexadecimal.

Sempre que a seleção muda, a página faz `postMessage` para o contentor:

```json
{
  "type": "floristhub:arrangement",
  "seed": 17483021,
  "shape": 0,
  "withVase": true,
  "baseType": 0,
  "items": [{ "name": "Rosa vermelha", "quantity": 3, "color": "Vermelho" }]
}
```

O alvo é `*`, porque a WebView terá outra origem. A mensagem não leva sessão nem dados de pagamento.

## Testes

- `web`: `npm test` (Vitest) cobre o gerador: mesma semente, semente diferente, formatos, vaso, tipos de flor e o limite de hastes.
- `backend`: validação dos novos campos e o formato do token.
- `npm run build` continua a ser a verificação de tipos e o bundle de produção.
