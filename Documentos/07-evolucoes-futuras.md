# Evoluções Futuras e O Que Gostaria de Ter Implementado

Este documento registra decisões de escopo e evoluções planejadas que não foram incluídas na implementação atual, por limitação de tempo ou por serem desnecessárias para o estágio atual do produto.

---

## O Que Foi Implementado (MVP)

O MVP atual inclui código funcional e executável dos seguintes componentes:

- **BFF (.NET 10):** ponto único do canal web com autenticação JWT, RBAC (`admin`/`merchant`), rate limiting, security headers e proxy para microserviços
- **Launch Service:** API REST com validações de negócio, persistência via EF Core, publicação de eventos via MassTransit/RabbitMQ, testes de unidade e integração
- **Daily Balance Worker:** Consumer MassTransit com lógica de consolidação idempotente, retry na fila, testes de unidade e integração (Testcontainers)
- **Daily Balance Service:** API REST somente-leitura com cache `IMemoryCache` (TTL 5 min), testes de unidade e integração
- **Frontend React:** SPA completa — login, lançamentos, consulta de saldo por data, gestão de usuários (admin), painel de relatórios de testes (`/tests`)
- **Observabilidade:** OpenTelemetry SDK (métricas + logs OTLP) → Collector → Prometheus + Loki → Grafana (dashboard básico)
- **Testes:** cobertura 100% linha/branch (xUnit + Testcontainers), E2E Playwright, carga k6 50 req/s

---

## O Que Gostaria de Ter Implementado com Mais Tempo

### 1. Arquitetura de Transição (Migração de Legado)

Caso a solução partisse de um sistema legado (ex.: planilha, sistema monolítico), a estratégia de migração seria:

**Fase 1 — Strangler Fig Pattern:**
- Criar o Launch Service em paralelo ao legado
- Interceptar chamadas de lançamento e duplicá-las para o novo serviço (shadow traffic)
- Validar consistência entre legado e novo sistema por no mínimo 2 semanas

**Fase 2 — Cutover Gradual:**
- Migrar dados históricos do legado para o PostgreSQL Launch via job de migração idempotente
- Redirecionar tráfego gradualmente (10% → 50% → 100%) com feature flag
- Manter legado em modo read-only durante a transição

**Fase 3 — Descomissionamento:**
- Validar integridade dos dados migrados
- Desligar o legado após período de convivência sem incidentes

Esta view de transição seria adicionada ao modelo C4 como **C4 Level 1 — Transition Architecture**.

---

### 2. Outbox Pattern para Garantia de Entrega

**Risco atual:** o Launch Service grava no banco e publica o evento em seguida, em duas operações separadas. Se o processo morrer entre as duas, o lançamento existe no banco mas o evento nunca chega ao Worker — o saldo não é consolidado.

A solução é o **Transactional Outbox Pattern**:

```
[ Launch Service ]
  ├── BEGIN TRANSACTION
  ├── INSERT INTO lancamentos (...)
  ├── INSERT INTO outbox_events (event_type, payload, status = 'pending')
  └── COMMIT

[ Outbox Processor (background) ]
  ├── SELECT * FROM outbox_events WHERE status = 'pending'
  ├── Publish to RabbitMQ
  └── UPDATE outbox_events SET status = 'published'
```

Garantia: a mensagem só é publicada após a transação ser confirmada. Implementável com **MassTransit Outbox** (suporte nativo via EF Core) — sem alteração de código no consumidor.

---

### 3. Cache Redis no Daily Balance Service

O MVP implementa `IMemoryCache` (in-process, TTL 5 min), suficiente para o volume atual. Para produção com múltiplas réplicas, o cache precisa ser distribuído:

```
[ Daily Balance Service — Réplica 1 ]
[ Daily Balance Service — Réplica 2 ]
        |
        └──> [ Redis ] (cache compartilhado entre réplicas)
                |
                └── Cache Miss --> [ PostgreSQL Balance ]
```

- **Redis** com TTL configurável (ex.: 60s — granularidade diária do saldo)
- Invalidação via evento do Worker após cada consolidação
- `IMemoryCache` atual seria substituído por `IDistributedCache` com Redis — sem alteração de lógica de negócio

---

### 4. Identity Provider Dedicado

Substituir o login local do BFF (`bff_db`) por um IdP externo quando houver necessidade de MFA, multi-tenancy ou políticas corporativas centralizadas:

- **Keycloak** (self-hosted, open-source): gestão de usuários, roles, OAuth2/OIDC completo
- Ou **Auth0** / **AWS Cognito** como serviço gerenciado
- O BFF passaria a validar tokens emitidos pelo IdP em vez de gerá-los localmente

---

### 6. Multi-tenancy

Para escalar o produto para múltiplos comerciantes:

- **Tenant ID** extraído do JWT e propagado em todos os serviços via header `X-Tenant-Id`
- **Row-Level Security (RLS)** no PostgreSQL por tenant
- Cada comerciante vê apenas seus próprios lançamentos e saldos

---

### 7. API de Relatórios / Exportação

- Exportação de lançamentos em CSV/Excel por período
- Relatório de fluxo de caixa projetado (baseado em histórico)
- Implementado como serviço separado de leitura (CQRS Read Model adicional)

---

### 8. Event Sourcing

Para um sistema financeiro com auditoria completa:

- Em vez de persistir o estado atual do saldo, persistir todos os eventos (`LaunchRegistered`) em um event store
- O saldo seria uma projection dos eventos
- Habilitaria time-travel queries e reprocessamento de saldo para qualquer data

Trade-off: maior complexidade operacional. Adequado para estágio de maturidade posterior.

---

## Roadmap Técnico Sugerido

| Fase | O Que | Quando |
|---|---|---|
| **MVP** (entregue) | BFF + Frontend + auth local + microserviços + OTel métricas/logs/traces (Tempo) | Sprint 1-3 |
| **Resiliência** | Outbox Pattern + Circuit Breaker explícito | Sprint 4 |
| **Observabilidade avançada** | Alertas Prometheus + SLO burn rate + dashboards K8s | Sprint 4 |
| **Performance** | Cache Redis distribuído no Daily Balance Service | Sprint 5 |
| **Segurança avançada** | Keycloak/OIDC + mTLS via Istio | Sprint 6+ |
| **Multi-tenancy** | Tenant ID + RLS no banco | Sprint 7-8 |
| **Relatórios** | Serviço de exportação e relatórios | Sprint 9+ |
