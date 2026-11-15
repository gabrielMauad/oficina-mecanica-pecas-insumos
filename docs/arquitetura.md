# Arquitetura do serviço PecasInsumos

Complementa o [README](../README.md) com a estrutura de projetos, os fluxos de mensageria em detalhe e
as decisões de projeto do serviço.

## Estrutura de projetos

```
src/Bootstrap/Api/                       host: Program.cs, autenticação, OpenAPI, health checks, observabilidade
src/Modules/PecasInsumos/
  PecasInsumos.Domain/                   PecaInsumo (agregado), Dinheiro, UnidadeDeMedida
  PecasInsumos.Application/              Commands, Queries, IntegrationEventHandlers, port IPecaInsumoGateway
  PecasInsumos.Adapters/                 PecaInsumoController (CA), Gateway, Presenter, Requests/ViewModels, IPecaInsumoRepository
  PecasInsumos.Infrastructure/           DbContext + migrations, repositório EF, consumers e bus MassTransit
  PecasInsumos.Web/                      PecaInsumoApiController (rotas HTTP e papéis)
src/SharedKernel/                        Result/Error, behaviors do MediatR, IIntegrationEventBus
src/Contracts/OficinaMecanica.Contracts  eventos de integração (cópia idêntica em todos os serviços)
```

Direção das dependências: `Web → Adapters → Application → Domain`; `Infrastructure` implementa os ports
declarados em `Adapters` e `Application`. `Domain` e `Application` não referenciam MassTransit, EF Core
nem ASP.NET.

## Pipeline do MediatR

Todo command passa por `LoggingBehavior` → `ValidationBehavior` → `TransactionBehavior`.

- A validação (FluentValidation) devolve uma falha `Result`, não lança exceção; no consumer isso significa
  que um item inválido do evento (id vazio, quantidade ≤ 0) vira falha de negócio.
- O `TransactionBehavior` só persiste (`SaveChanges`) quando o resultado é sucesso, e publica os eventos
  pendentes **depois** do commit. Um command que falha não grava nada.

## Fluxo 1 — baixa do orçamento (`OrcamentoGerado`)

1. A OS publica `OrcamentoGerado` com `OrdemServicoId`, `OrcamentoId` e a lista `{PecaInsumoId, Quantidade}`.
2. O `OrcamentoGeradoConsumer` (fila `pecas-insumos-orcamento-gerado`) delega ao
   `DecrementarEstoqueQuandoOrcamentoGerado`, que envia `BaixarEstoqueDoOrcamentoCommand`.
3. `AjusteDeEstoqueEmLote` soma as quantidades por peça e entrega o lote ao gateway, que numa **única
   transação** registra o orçamento na tabela `movimentacao_orcamento` (chave única por orçamento e tipo)
   e aplica, peça a peça e em ordem fixa de id, um `UPDATE` condicional
   (`quantidade_estoque = quantidade_estoque - @q WHERE id = @id AND ativo AND quantidade_estoque >= @q`).
   O banco avalia a condição sobre o saldo corrente, então baixas concorrentes nunca passam do saldo.
4. Todas aplicadas: `COMMIT`. O orçamento já registrado (reentrega ou entrega simultânea) não baixa nada e
   é tratado como sucesso.
5. Alguma peça inexistente, desativada ou sem saldo: `ROLLBACK` de tudo (inclusive do registro), o command
   falha acumulando o motivo de **todas** as peças problemáticas e o handler publica `EstoqueBaixaFalhou`.
   Como o `TransactionBehavior` não publica eventos quando o command falha, a publicação é feita
   diretamente no `IIntegrationEventBus` pelo handler do evento.

## Fluxo 2 — estorno (`OrcamentoRejeitado`)

Mesmo mecanismo em lote, com `UPDATE` condicional de soma (respeitando o limite de `int.MaxValue`), sem exigir
peça ativa, e registro (`orcamento_id`, `Estorno`). Todas as peças são devolvidas numa transação ou nenhuma, e
um `OrcamentoRejeitado` repetido ou simultâneo devolve o estoque uma única vez. O estorno só devolve estoque de
um orçamento com baixa registrada: sem ela, grava apenas a linha `Estorno` (que faz uma baixa posterior ser
ignorada) e não mexe no estoque. Toda movimentação do mesmo orçamento é serializada por um lock consultivo
transacional do PostgreSQL (`pg_advisory_xact_lock`). Peça inexistente ou com saldo que passaria do limite
impede o estorno inteiro, que fica registrado em log de erro.

## Fluxo 3 — consulta de disponibilidade (HTTP)

`GET /api/v1/pecas-insumos/{id}/disponibilidade?quantidade={n}` devolve `disponivel` (peça ativa e saldo ≥ `n`) e o
`precoUnitario` atual. É uma leitura, sem reserva: a reserva efetiva acontece só quando o orçamento é
gerado e o evento é consumido (fluxo 1).

## Decisões de projeto

| Decisão | Razão |
|---|---|
| Baixa e estorno do orçamento por **evento**, não por chamada HTTP da OS | a OS não precisa aguardar o estoque nem conhecer a API de saída de estoque; o serviço decide e comunica só a falha |
| Operação **em lote, tudo ou nada** | um orçamento parcialmente baixado deixaria o estoque inconsistente sem ter como compensar peça a peça |
| Falha de negócio vira **evento** (`EstoqueBaixaFalhou`), falha de infraestrutura vira **exceção** | o evento alimenta a compensação da Saga (OS rejeita o orçamento; pagamento cancela o pendente); a exceção aciona o retry do consumer |
| Banco **PostgreSQL** próprio | dados relacionais simples e transacionais (saldo de estoque exige gravação atômica de várias linhas) |
| Saldo alterado por **`UPDATE` condicional**, não por ler-alterar-gravar | duas movimentações concorrentes que leem o mesmo saldo perdem uma atualização (e vendem estoque que não existe); o `UPDATE` trava a linha e reavalia o saldo corrente, sem retry nem token de concorrência |
| Baixa e estorno **idempotentes por orçamento** (tabela `movimentacao_orcamento`), estorno só de orçamento com baixa registrada | a mensageria entrega ao menos uma vez e a OS pode publicar o mesmo evento duas vezes; sem o registro, o estoque seria baixado ou devolvido duas vezes. O estorno que chega sem baixa (a baixa falhou) não pode inflar o estoque, e a linha de estorno impede que uma baixa tardia consuma o estoque de um orçamento já rejeitado |
| Consumers MassTransit finos | mantêm a Application independente do transporte; trocar o broker não altera Domain nem Application |
| Sem chamadas HTTP de saída | o evento já carrega peças e quantidades, então o consumo de fila não depende de outro serviço estar no ar |

## Limitações e evolução futura

Descritas no [README](../README.md#limitações-conhecidas): sem outbox, idempotência apenas de baixa e estorno,
estorno sem compensação e orçamentos anteriores ao registro de movimentações sem estorno automático.
