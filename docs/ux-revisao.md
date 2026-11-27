# Revisão de UX — Florist Hub

Inventário feito a partir de `main` (`75fbdbe`) e da loja em produção (São Flores). As cores da loja (`--store-*`, São Flores `#8E3B5C` / `#E8B4BC`) e a sidebar do admin `#1e3d32` mantêm-se.

## Loja pública

| Ecrã | Problema | Solução |
| --- | --- | --- |
| Cabeçalho | O botão de tema/idioma fica solto a meio da barra vinho, sem superfície nem popover alinhado ao ícone. | Grelha marca / navegação / botão à direita. Popover de superfície, sombra, cantos, secções, visto no activo, ícones sol/lua/monitor. Fecha com Esc e clique fora. |
| Marca | «Florist Hub» por baixo do nome da loja (quando o subtítulo ainda era renderizado). | Só o nome da loja na loja pública. |
| Primeira visita | Antes do tema da API, botões e acentos caem no verde genérico. | Aplicar o último tema guardado para o slug antes da primeira pintura e voltar a gravá-lo quando a API responde. |
| Início, portfolio, encomendas | «Usar como base» e «Ver detalhe e linha do tempo» parecem ligações. | Botões reais (`fh-btn`), com o tenant preservado no URL. |
| `/flores` | Marcador de lista (ponto) ao lado dos cartões. «Indisponível neste momento» em flores com stock e catálogo activo (`isApproximatelyAvailable: false` por omissão do onboarding). | `list-style: none`. Indisponível só sem stock ou fora do catálogo. A listagem pública da API reporta a flor como encomendável quando há stock. |
| `/montar` sem `?tenant=` | Os `NavLink` perdem o slug e o catálogo chega vazio. | `withTenantSearch` usa também o `?tenant=` da página actual. |

## Montar arranjo

| Problema | Solução |
| --- | --- |
| `<select>` nativo de formato e texto «esponja · esponja, sem vaso». | Formato e vaso em chips. Esponja não oferece vaso. |
| Stepper e ainda «PASSO 1» no título. | Um único stepper. |
| 3D por cima e flores por baixo; no telemóvel o total desaparece. | Desktop: palco e flores lado a lado. Telemóvel: barra fixa com total e Continuar (desactivado se o cesto está vazio). |
| Disco bege enorme e espuma low-poly. | Palco do tema, sombra de contacto, espuma verde, pétalas curvas, PBR, HDRI próprio CC0. |
| Sucesso pouco visível. | Faixa de sucesso com link de acompanhamento. O rascunho já grava o passo de entrega. |

## Admin e SuperAdmin

| Ecrã | Problema | Solução |
| --- | --- | --- |
| Sidebar | Em alguns temas os botões «Conta» e «Sair» herdam o estilo global e ficam ilegíveis; o atalho «CONTA» em maiúsculas ainda existe no CSS antigo. | Botões com especificidade própria: inicial, email truncado, «Conta» e «Sair». |
| Modo claro | Conteúdo do admin sem fundo explícito e controlos nativos. | Fundo `--fh-bg`, cartões e listboxes com contraste de texto. |
| Capacidade | Tabela «data / ocupação / vagas» repetida. | Calendário do mês, cor livre / quase cheio / cheio, dias passados esbatidos, clique para limite, nota e fechar, resumo de 7 dias, limite diário padrão. |
| Dashboard | «Novidades» só aparece se já houver notificações. | Secção sempre visível, com estado vazio. |
| Catálogo | Miniaturas já existem na grelha; faltava garantir o enquadramento. | Miniatura 40px com cantos, ao lado do nome. |
| Detalhe da loja (SuperAdmin) | Nome, cores, letra, acções e comissão na mesma coluna; «Tipo de letra» é um `<select>`. | Separadores e zona de acções perigosas à parte. |
| Listas (encomendas, faturas, cupões, recorrências) | `<select>` nativo. | Listbox com popover. |

## App móvel

O ecrã de preferências é uma lista de rádios numa página própria. O mesmo menu (tema e idioma) passa a abrir a partir do topo, com superfície e visto no activo.

## Fora deste ciclo

- Hero da loja continua a usar a imagem que a API enviar.
- Não há modelos glTF de flores de terceiros no repositório: o ramo é geometria própria (CC0) com caminho para GLB Draco/meshopt.
- Paginação já existe no catálogo; outras listas muito longas ficam com o filtro em listbox, sem novos endpoints.
