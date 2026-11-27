# Imagens de demonstração (seed)

Ficheiros em `backend/src/FloristHub.Api/demo-assets/` copiados para `uploads/demo/` em desenvolvimento (`DevelopmentDemoAssetsInstaller`).

Cada ficheiro foi **revisto visualmente** antes de commit (conteúdo floral ou placeholder neutro explícito; **sem pessoas**).

| Ficheiro | Uso no seed | O que mostra | Origem | Licença / autor |
|----------|-------------|--------------|--------|-----------------|
| `flower-tulipa-amarela.jpg` | «Túlipa amarela» | Túlipa amarela em close-up | [Wikimedia Commons: Yellow Tulip Flower](https://commons.wikimedia.org/wiki/File:Yellow_Tulip_Flower.jpg) | [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) — autor na página do ficheiro |
| `flower-rosa-vermelha.jpg` | «Rosa vermelha» | Rosa vermelha com gotas de água | [Flickr (via Openverse)](https://www.flickr.com/photos/74121623@N08/16459677541/) | Ver licença na página Flickr — **Kumar's Edit** (atribuir conforme indicado pelo autor) |
| `flower-girassol.jpg` | «Girassol» | Girassol contra céu azul | [Wikimedia Commons: Sunflower sky backdrop](https://commons.wikimedia.org/wiki/File:Sunflower_sky_backdrop.jpg) | [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) — Steve Jurvetson |
| `flower-eucalipto.jpg` | «Eucalipto» | Folhas de eucalipto | [Flickr (via Openverse)](https://www.flickr.com/photos/31064740@N05/3494704905/) | Ver licença na página Flickr |
| `flower-lavanda-selvagem.jpg` | «Lavanda selvagem» | Inflorescência de lavanda (*Lavandula angustifolia*) em macro | [Wikimedia Commons: Lavandula angustifolia - lavender - Lavendel - 01](https://commons.wikimedia.org/wiki/File:Lavandula_angustifolia_-_lavender_-_Lavendel_-_01.jpg) | [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) — Norbert Nagel |
| `flower-margarida-silvestre.jpg` | «Margarida silvestre» | Margarida-branca (*Leucanthemum vulgare*) | [Wikimedia Commons: Leucanthemum vulgare](https://commons.wikimedia.org/wiki/File:Leucanthemum_vulgare.jpeg) | [CC BY-SA 2.5 ca](https://creativecommons.org/licenses/by-sa/2.5/ca/deed.en) — The Cosmonaut |
| `bouquet-primaveril.jpg` | Portfolio «Buquê primaveril» | Buquê de flores (rosas, peónias, magnólia) | [Pexels #931162](https://www.pexels.com/photo/pink-roses-bouquet-on-white-table-931162/) | [Pexels License](https://www.pexels.com/license/) |
| `flores-lina-logo.jpg` | Logo tenant `flores-lina` | Campo de papoilas-de-califórnia (laranja) | [Unsplash](https://unsplash.com/photos/orange-flowers-field-1490750967868) | [Unsplash License](https://unsplash.com/license) |
| `flores-do-campo-logo.jpg` | Logo tenant `flores-do-campo` | Prado com flores silvestres e montanhas | [Wikimedia Commons: Wildflowers meadow](https://commons.wikimedia.org/wiki/File:Wildflowers_meadow.jpg) | Ver licença na página do ficheiro |

## Histórico de correção (PR #17)

- **Incorretas no PR original (removidas):** vários IDs Pexels apontavam para **fotos de pessoas** ou paisagens (`flower-tulipa-amarela`, `flower-girassol`, etc.) — nunca validados visualmente.
- **Incorretas já em `main` antes deste PR:** `flores-lina-logo.jpg` era **paisagem costeira** (sem flores nem produto floral). `bouquet-primaveril.jpg` estava **correto** (buquê, sem pessoas). `flores-do-campo-logo.jpg` em `main` era um buquê válido; foi substituído por prado silvestre para o logótipo bater certo com o nome da loja.

Não substituir por imagens com direitos restritos ou conteúdo humano sem rever esta tabela e o seed.
