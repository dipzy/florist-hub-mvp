# Créditos 3D

O ramo do configurador é geometria própria do Florist Hub (pétalas curvas por extrusão, por espécie: rosa em espiral, tulipa em cálice, girassol com disco, hortênsia em cachos, alstroeméria com manchas, folhas nos caules, espuma, jarra de vidro e embrulho kraft). Licença: CC0. Não há modelos de pessoas nem GLB de terceiros.

## Iluminação

`web/public/hdri/studio-cc0.hdr` é um estúdio equirectangular de 64×32 gerado no projeto (céu quente e preenchimento frio). CC0.

## Cor

Cada flor usa a cor do catálogo (`color` ou o nome) no `MeshPhysicalMaterial`, com sheen baixo. Um mapa bege por cima dessa cor apagava o tom (o girassol ficava creme), por isso não há textura de fotografia nas pétalas. Não há imagens de pessoas.

## GLB opcional

`FlowerMesh` aceita um URL `.glb` / `.gltf`. O carregador usa o descodificador Draco em `web/public/draco/` (binários do projeto three.js, licença Apache-2.0) e o descodificador meshopt incluído no three.js. Se o ficheiro falhar, volta à flor procedural. Sem WebGL, o configurador desenha o mesmo arranjo em 2D.
