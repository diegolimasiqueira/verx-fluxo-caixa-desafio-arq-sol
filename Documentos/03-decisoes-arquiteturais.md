# Decisões Arquiteturais (ADRs)

## ADR-001 — Padrão Arquitetural: Microserviços com Event-Driven

**Status:** Aceito

**Contexto:**
O desafio exige que o serviço de lançamentos permaneça disponível mesmo quando o serviço de consolidado diário estiver fora do ar. Um monolito tornaria os dois domínios indissociáveis em runtime.

**Decisão:**
Adotar arquitetura de **microserviços** com comunicação **assíncrona via broker de mensagens** entre os domínios de lançamentos e consolidação.

**Consequências:**
- Lançamentos são registrados e confirmados sem aguardar consolidação
- Falha no consolidador não impede novos lançamentos
- Complexidade operacional maior (múltiplos serviços, broker, dois bancos)
- Requer infraestrutura de mensageria e observabilidade distribuída

**Alternativas descartadas:**
- **Monolito:** atenderia o problema de forma mais simples, mas inviabiliza o isolamento de falhas exigido pelo requisito RNF-01
- **Chamada síncrona REST entre serviços:** cria acoplamento temporal — se o Daily Balance cair, o Launch teria que lidar com falhas de integração

---

## ADR-002 — CQRS: Separação de Bancos de Dados por Domínio

**Status:** Aceito

**Contexto:**
O domínio de lançamentos tem padrão de acesso transacional (escrita + consistência forte). O domínio de saldo diário tem padrão de leitura intensiva com até 50 req/s em pico.

**Decisão:**
Adotar **CQRS (Command Query Responsibility Segregation)** com bancos separados:
- **PostgreSQL Launch:** banco transacional, normalizado, com índices para escrita e consulta por data
- **PostgreSQL Balance:** banco otimizado para leitura, com desnormalização do saldo consolidado por data

**Consequências:**
- Leitura de saldo não compete com escrita de lançamentos por recursos de banco
- Saldo consolidado é eventualmente consistente (atualizado via worker assíncrono)
- Dois bancos para operar e monitorar
- Estratégia de backup independente por domínio

**Alternativas descartadas:**
- **Banco único compartilhado:** cria contenção de recursos e acoplamento de schema entre domínios
- **Redis como banco primário de saldo:** ganho de performance, mas sem durabilidade transacional adequada para dados financeiros

---

## ADR-003 — Broker de Mensagens: RabbitMQ

**Status:** Aceito

**Contexto:**
Necessário desacoplar o registro de lançamentos da consolidação. O volume esperado não justifica uma plataforma de streaming como Kafka.

**Decisão:**
Usar **RabbitMQ** como broker de mensagens para publicação/consumo do evento `LaunchRegistered`.

**Justificativa:**
- Volume de mensagens proporcional ao volume de lançamentos (não é streaming de alta escala)
- Suporte nativo a Dead Letter Queue (DLQ) e retry com backoff
- Operação mais simples que Kafka para o problema em questão
- Amplamente suportado no ecossistema .NET (MassTransit, RabbitMQ.Client)
- Clustering disponível para alta disponibilidade

**Consequências:**
- Garantia de entrega at-least-once — o worker deve ser idempotente
- DLQ captura mensagens que falham repetidamente para análise
- Monitorar profundidade da fila como SLI de lag de consolidação

**Alternativas descartadas:**
- **Kafka:** over-engineering para o volume esperado; maior complexidade operacional
- **Azure Service Bus / SQS:** introduz dependência de nuvem específica; RabbitMQ é agnóstico

---

## ADR-004 — Runtime: Kubernetes com KEDA e HPA

**Status:** Aceito

**Contexto:**
A solução precisa escalar horizontalmente para absorver picos de 50 req/s no consolidado e processar fila de eventos sem lag.

**Decisão:**
Executar todos os serviços em **Kubernetes** com:
- **HPA (Horizontal Pod Autoscaler):** escala Launch Service e Daily Balance Service com base em CPU/memória
- **KEDA (Kubernetes Event-Driven Autoscaling):** escala o Daily Balance Worker com base na profundidade da fila RabbitMQ

**Justificativa:**
- KEDA é a solução padrão para escalonamento baseado em eventos em Kubernetes
- HPA cobre o escalonamento reativo por métricas de runtime
- Kubernetes fornece auto-healing, rolling deployments e isolamento de carga

**Consequências:**
- Necessário definir `minReplicas` e `maxReplicas` por serviço
- Worker deve ser stateless e idempotente para suportar múltiplas réplicas simultâneas
- Requer monitoramento da profundidade da fila como métrica de escalonamento

---

## ADR-005 — Service Mesh: Istio

**Status:** Aceito (opcional em desenvolvimento, obrigatório em produção)

**Contexto:**
Comunicação interna entre serviços precisa de mTLS, circuit breaker e rastreamento distribuído sem implementação manual em cada serviço.

**Decisão:**
Usar **Istio Service Mesh** para:
- mTLS automático entre pods
- Circuit breaker e retry policies declarativos
- Tracing distribuído integrado ao OpenTelemetry
- Traffic management (canary, A/B)

**Consequências:**
- Overhead de sidecar (Envoy proxy) por pod (~50-100MB RAM)
- Curva de aprendizado operacional
- Elimina necessidade de implementar circuit breaker em código de aplicação

**Alternativas descartadas:**
- **Implementação manual (Polly):** possível, mas duplica lógica em cada serviço; Istio centraliza as políticas
- **Linkerd:** mais leve, mas menos recursos que Istio para o nível de controle desejado

---

## ADR-006 — Linguagem e Framework: .NET 10

**Status:** Aceito

**Contexto:**
Necessário escolher o stack para os microserviços backend.

**Decisão:**
Usar **.NET 10** com **ASP.NET Core** para os serviços e **Worker Service** para o consumidor assíncrono.

**Justificativa:**
- Performance competitiva (Kestrel é um dos servidores HTTP mais rápidos em benchmarks TechEmpower)
- Ecosystem maduro para microserviços: MassTransit, EF Core, OpenTelemetry SDK, Aspire
- Long-term support (LTS)
- Suporte nativo a gRPC, REST, background workers e health checks
- Forte tipagem e tooling (test, profiling, containers)

---

## ADR-007 — BFF (Backend for Frontend)

**Status:** Aceito

**Contexto:**
O frontend React consome dois domínios diferentes (lançamentos e saldo). Sem um BFF, o frontend precisaria conhecer os endereços de cada microserviço, lidar com CORS e tokens individualmente.

**Decisão:**
Introduzir um **BFF (.NET 10)** como ponto único de entrada para o canal web.

**Responsabilidades do BFF:**
- Validação e propagação de JWT
- Roteamento (proxy) para Launch Service e Daily Balance Service
- Rate limiting por usuário/IP
- Security headers e políticas de CORS centralizadas
- Gerenciamento de usuários (`bff_db`)

> **MVP:** o BFF funciona como proxy — roteia chamadas individualmente para cada microserviço. Aggregation de múltiplas chamadas em uma única resposta é evolução futura.

**Consequências:**
- O BFF se torna um ponto crítico — deve ter alta disponibilidade e escala independente
- Não deve conter lógica de negócio — apenas orquestração de chamadas

---

## Resumo das Escolhas Tecnológicas

| Componente | Tecnologia | Justificativa |
|---|---|---|
| Frontend | React | Ecossistema, componentização, SPA |
| BFF / Services | .NET 10 / ASP.NET Core | Performance, LTS, ecosystem |
| Worker | .NET 10 Worker Service | Background processing nativo |
| Banco transacional | PostgreSQL | ACID, open-source, confiabilidade |
| Banco de leitura | PostgreSQL | Consistência, familiaridade operacional |
| Broker | RabbitMQ | Simples, confiável, DLQ nativa |
| Orquestração | Kubernetes | Portabilidade, escalonamento, resiliência |
| Autoscaling | KEDA + HPA | Queue-depth + CPU/mem scaling |
| Service Mesh | Istio | mTLS, circuit breaker, tracing |
| Observabilidade | OTel + Prometheus + Loki + Grafana | Stack open-source consolidado |
