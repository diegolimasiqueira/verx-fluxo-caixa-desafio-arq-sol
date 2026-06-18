# Evoluções Futuras e O Que Gostaria de Ter Implementado

Este documento registra decisões de escopo e evoluções planejadas que não foram incluídas na implementação atual, por limitação de tempo ou por serem desnecessárias para o estágio atual do produto.

---

## O Que Gostaria de Ter Implementado

### 1. Arquitetura de Transição (Migração de Legado)

Caso a solução partisse de um sistema legado (ex.: planilha, sistema monolítico ou serviço legado de controle financeiro), a estratégia de migração seria:

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

### 2. Implementação Completa dos Serviços

A implementação atual é parcial (arquitetural/documental). O que seria implementado com mais tempo:

- **Launch Service:** API REST completa com validações, persistência via EF Core, publicação de eventos com MassTransit (RabbitMQ), testes de unidade e integração
- **Daily Balance Worker:** Consumer MassTransit, lógica de consolidação idempotente, tratamento de DLQ
- **Daily Balance Service:** API REST de leitura com paginação e cache em memória (IMemoryCache ou Redis)
- **BFF:** Aggregation de chamadas, validação JWT, rate limiting com `AspNetCoreRateLimit`
- **Frontend React:** Formulário de lançamentos, listagem por data e exibição de saldo diário

---

### 3. Cache de Leitura no Daily Balance Service

Para suportar 50 req/s com latência < 300ms, a estratégia de cache seria:

```
[ Daily Balance Service ]
        |
        ├── Cache Hit  --> [ Redis ] (< 5ms)
        └── Cache Miss --> [ PostgreSQL Balance ] --> atualiza Redis
```

- **Redis** como cache de segunda camada para saldos consolidados
- TTL de 60 segundos (saldo tem granularidade diária, atualização assíncrona)
- Invalidação de cache via evento do worker após cada consolidação
- Reduz carga no banco em ~80% nos picos

---

### 4. Outbox Pattern para Garantia de Entrega

O risco atual: se o Launch Service gravar no banco mas falhar antes de publicar o evento no RabbitMQ, a consolidação não ocorre.

A solução seria o **Transactional Outbox Pattern**:

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

Garantia: a mensagem só é publicada após a transação ser confirmada. Implementável com **MassTransit Outbox** (suporte nativo via EF Core).

---

### 5. Identity Provider Dedicado

Em vez de validar tokens JWT diretamente no BFF, integrar um IdP:

- **Keycloak** (self-hosted, open-source): gestão de usuários, roles, OAuth2/OIDC completo
- Ou **Auth0** / **AWS Cognito** como serviço gerenciado
- Permite multi-tenancy (múltiplos comerciantes), MFA e políticas de senha centralizadas

---

### 6. Multi-tenancy

Para escalar o produto para múltiplos comerciantes:

- **Tenant ID** extraído do JWT e propagado em todos os serviços via header `X-Tenant-Id`
- **Row-Level Security (RLS)** no PostgreSQL por tenant
- Ou schema separado por tenant (tenant isolation mais forte, custo operacional maior)
- Cada comerciante vê apenas seus próprios lançamentos e saldos

---

### 7. API de Relatórios / Exportação

Capacidade adicional de negócio para o futuro:

- Exportação de lançamentos em CSV/Excel por período
- Relatório de fluxo de caixa projetado (baseado em histórico)
- Implementado como serviço separado de leitura (CQRS Read Model adicional) para não impactar o serviço de consulta principal

---

### 8. Event Sourcing

Para um sistema financeiro, manter o log imutável de eventos como fonte de verdade:

- Em vez de persistir o estado atual do saldo, persistir todos os eventos (`LaunchRegistered`) em um event store
- O saldo seria um projection dos eventos
- Habilitaria auditoria completa, time-travel queries e reprocessamento de saldo para qualquer data

Trade-off: maior complexidade operacional e de desenvolvimento. Adequado para estágio de maturidade posterior.

---

## Roadmap Técnico Sugerido

| Fase | O Que | Quando |
|---|---|---|
| **MVP** | Launch Service + Daily Balance Worker + Daily Balance Service (sem BFF/Frontend) | Sprint 1-2 |
| **Canal** | BFF + Frontend React básico | Sprint 3 |
| **Resiliência** | Outbox Pattern + DLQ + Circuit Breaker explícito | Sprint 4 |
| **Performance** | Cache Redis no Daily Balance Service | Sprint 5 |
| **Segurança** | Keycloak + mTLS completo via Istio | Sprint 6 |
| **Multi-tenancy** | Tenant ID + RLS no banco | Sprint 7-8 |
| **Relatórios** | Serviço de exportação e relatórios | Sprint 9+ |
