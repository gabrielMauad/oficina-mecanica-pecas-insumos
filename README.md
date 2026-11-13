# oficina-mecanica-pecas-insumos

Microsserviço de **peças e insumos** (estoque) da oficina mecânica. Tem banco PostgreSQL próprio
(`pecas_insumos`), expõe a API REST do catálogo/estoque e reage por mensageria aos eventos do fluxo da
ordem de serviço. Extraído do módulo `PecasInsumos` do monólito (`oficina-mecanica-app`), mantendo
Clean Architecture (`Domain` → `Application` → `Adapters` → `Infrastructure`/`Web`; host em `src/Bootstrap/Api`).

## API

Todas as rotas exigem JWT com papel `Oficina` (`Authorization: Bearer ...`; segredo compartilhado
`Jwt__Secret`). Documentação em `/scalar` e `/openapi/v1.json`; health checks em `/healthz`,
`/healthz/live` e `/healthz/ready`.

| Rota | Descrição |
|---|---|
| `POST /api/v1/pecas-insumos` | cadastra peça/insumo |
| `GET /api/v1/pecas-insumos` · `GET /api/v1/pecas-insumos/{id}` | lista / obtém |
| `PATCH /api/v1/pecas-insumos/{id}/descricao` · `/preco` | atualiza descrição / preço |
| `PATCH /api/v1/pecas-insumos/{id}/estoque/entrada` · `/saida` | entrada / saída de estoque |
| `DELETE /api/v1/pecas-insumos/{id}` | desativa |
| `GET /api/v1/pecas-insumos/{id}/disponibilidade?quantidade={n}` | `200 {"disponivel": bool, "precoUnitario": decimal}`; `404` se a peça não existe; `422` se `quantidade` ≤ 0 |

A rota de disponibilidade é chamada por HTTP pelo serviço de ordens de serviço.

## Mensageria

MassTransit 8.x sobre Amazon SQS/SNS (filas `pecas-insumos-*`, criadas automaticamente). Retry de
consumo: 1 s, 5 s e 10 s. Os tipos das mensagens ficam em `src/Contracts/OficinaMecanica.Contracts`
(namespace `OficinaMecanica.Contracts.IntegrationEvents`, cópia idêntica em todos os serviços).

| Evento consumido | Efeito |
|---|---|
| `OrcamentoGeradoIntegrationEvent` | baixa de estoque das peças do orçamento |
| `OrcamentoRejeitadoIntegrationEvent` | estorno de estoque das peças do orçamento |

Os consumers (`PecasInsumos.Infrastructure/Messaging/Consumers`) apenas delegam aos handlers da
Application. A Application publica por `IIntegrationEventBus`, implementado sobre `IPublishEndpoint`
(`MassTransitIntegrationEventBus`).

A baixa de estoque do orçamento é **atômica**: todas as peças ou nenhuma, em um único `SaveChanges`.
Se falhar por motivo de negócio (peça inexistente, desativada, sem estoque ou item inválido), o serviço
publica `EstoqueBaixaFalhouIntegrationEvent`. Falhas de infraestrutura lançam exceção e seguem o retry do
consumer. O estorno também é atômico, mas uma falha de estorno só é registrada em log.

Configuração (`Mensageria__*`): `Transporte` (`AmazonSqs` padrão, ou `InMemory`), `Regiao`
(`us-east-1`), `ServiceUrl`, `AccessKey` e `SecretKey` (só em ambiente local com LocalStack; vazios na AWS).

### Limitações conhecidas

- **Concorrência**: a peça não tem token de concorrência; duas baixas simultâneas da mesma peça podem
  perder uma atualização (lost update). Evolução futura: token de concorrência (`xmin`) no registro da
  peça, com o conflito caindo no retry do consumer.
- **Reentrega**: não há deduplicação por `EventId` (sem inbox). Uma reentrega de `OrcamentoGerado` ou
  `OrcamentoRejeitado` baixa ou estorna de novo; numa reentrega de baixa já aplicada, o estoque pode ficar
  insuficiente e gerar um `EstoqueBaixaFalhou` indevido, fazendo a OS rejeitar o orçamento sem estornar o
  que a primeira entrega baixou. Evolução futura: tabela de mensagens processadas (inbox).

## Como rodar localmente

Pré-requisitos: Docker e .NET 10 SDK.

1. No repositório `oficina-mecanica-app`, suba as dependências compartilhadas (rede `oficina-local`,
   LocalStack e Jaeger): `docker compose -f docker-compose.infra.yml up -d`.
2. Neste repositório: `docker compose up --build`.

A API fica em <http://localhost:8083> (Scalar em `/scalar`) e o Postgres em `localhost:5434`
(database `pecas_insumos`, usuário `oficina`). Traces no Jaeger: <http://localhost:16686>.

## Testes

```bash
dotnet build OficinaMecanica.PecasInsumos.slnx
dotnet test OficinaMecanica.PecasInsumos.slnx
```

Os testes de integração (`tests/IntegrationTests`) usam Testcontainers (PostgreSQL) e exigem Docker; a
mensageria usa o transporte `InMemory` com o test harness do MassTransit. Para rodar só os unitários:
`dotnet test OficinaMecanica.PecasInsumos.slnx --filter "FullyQualifiedName!~IntegrationTests"`.

## CI

`.github/workflows/ci.yml` roda em pull requests para `main` (job `Build & Test`): restore, build e testes
da solution.
