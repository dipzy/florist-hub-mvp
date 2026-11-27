# Capacidade de entrega e stock de flores

Cada loja passa a poder limitar quantas encomendas aceita por dia e, se quiser, por janela horária. O stock de cada flor do catálogo pode ser controlado: a confirmação consome, o cancelamento ou o reembolso total repõe.

Enquanto a florista não gravar um limite, a loja continua sem teto. As encomendas antigas não são reescritas.

## Capacidade

- O limite diário vai de 0 a 500. Zero significa que o dia não aceita encomendas.
- As janelas são opcionais, no máximo oito, sem sobreposição. O início e o fim são inclusivos. Exemplo: `09:00–11:59` e `12:00–19:00`.
- Uma exceção de data fecha o dia (folga ou feriado) ou substitui o limite (dia de pico).
- A vaga só é reservada quando a encomenda traz data pretendida e a loja já tem política. A reserva sai quando a encomenda é cancelada ou recusada. Uma encomenda gerada por recorrência reserva a vaga da mesma forma. Se o dia estiver cheio, fechado ou fora da janela, a ocorrência fica saltada, a próxima data avança e a florista e o cliente são avisados por email e push. O stock só é descontado quando a florista confirma essa encomenda.
- O checkout e a API recusam datas e horas sem vaga. Duas encomendas a disputar a última vaga ficam serializadas por loja e data; em Postgres usa-se também um `pg_advisory_xact_lock`.
- Sem política gravada, criar encomenda não verifica capacidade, para as lojas de demonstração e os testes existentes continuarem a funcionar.

Rotas da florista, sempre filtradas pelo tenant:

- `GET /api/florist/capacity?from&to`
- `GET` e `PUT /api/florist/capacity/settings`
- `PUT /api/florist/capacity/exceptions`
- `DELETE /api/florist/capacity/exceptions/{data}`

O cliente lê `GET /api/store/capacity?from&to`. A resposta pública não inclui a nota interna da exceção.

No painel web, a página Capacidade mostra o calendário do mês e permite gravar o limite, as janelas e as exceções. No checkout, um dia cheio aparece como «sem vaga» e não pode ser escolhido. Na app, a florista vê a ocupação do dia no painel e o cliente só escolhe datas com vaga.

## Stock

`StockQuantity` nulo continua a significar stock não controlado. `MinimumStockQuantity` é o limiar de aviso.

- Confirmar a encomenda, ou aceitar uma proposta que a confirma, desconta as flores usadas e grava um movimento.
- Cancelar ou reembolsar a encomenda na totalidade repõe o stock uma única vez. Um reembolso parcial não repõe.
- Se não houver quantidade suficiente, a confirmação falha e o stock fica como estava.
- O aviso de stock baixo dispara só quando a flor atravessa o mínimo (estava acima ou sem limiar e passa a ficar abaixo). O email usa a fila Resend já existente. O push reutiliza a fila de notificações com o tipo `low-stock` e a preferência «Stock baixo», ligada por omissão. Não há push web.
- Uma flor com stock controlado a zero ou negativo aparece como esgotada no catálogo, no configurador, nas campanhas e nas flores da época. Não pode ser comprada com preço de campanha nem com cupão. As encomendas já criadas mantêm as linhas.

Movimentos: `GET /api/flowers/{id}/movements`. Alertas da loja: `GET /api/flowers/alerts`. Outro tenant recebe 404 nos movimentos e não vê os alertas.

## Migração

A migração de capacidade e stock segue `20261110172215_PushNotifications`. A migração duplicada `20261110162901_PushNotifications` foi removida porque partilhava o nome da classe com a migração correta e impedia a compilação.
