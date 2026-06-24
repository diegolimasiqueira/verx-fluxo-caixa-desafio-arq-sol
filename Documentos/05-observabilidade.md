# Monitoramento e Observabilidade

## Stack Implementada (MVP)

```
[ Serviços .NET (BFF, Launch, Balance, Worker) ]
       | OTLP gRPC (métricas + logs + traces)
       v
[ OpenTelemetry Collector ]
       |
       ├──> [ Prometheus :8889 ]  ──> [ Grafana :3001 ]
       ├──> [ Loki :3100 ]        ──> [ Grafana :3001 ]
       └──> [ Tempo :4317 ]       ──> [ Grafana :3001 ]
```

Todos os serviços exportam via **OpenTelemetry SDK** (.NET) diretamente para o Collector (OTLP sobre gRPC na porta 4317). O Collector centraliza o roteamento para os backends.

**Serviços não expõem `/metrics` diretamente** — o endpoint de scrape Prometheus é `otel-collector:8889`.

---

## O Que Está Instrumentado (MVP)

### Métricas (Prometheus)

Instrumentação automática (via OpenTelemetry SDK):

| Fonte | Exemplos de métricas |
|---|---|
| ASP.NET Core | `http_server_request_duration_seconds` (histogram), `http_server_active_requests` |
| HttpClient | `http_client_request_duration_seconds` (histogram) |
| .NET Runtime | GC, memória, threads, exceções |

Métricas de negócio customizadas (`CashFlow.Platform` meter):

| Métrica | Tipo | Descrição |
|---|---|---|
| `cashflow_launch_registrations_total` | Counter | Lançamentos registrados com sucesso |
| `cashflow_launch_validation_errors_total` | Counter | Erros de validação de lançamento |
| `cashflow_daily_balance_queries_total` | Counter | Consultas de saldo consolidado |
| `cashflow_worker_consolidations_total` | Counter | Eventos consolidados pelo worker |
| `cashflow_worker_consolidation_errors_total` | Counter | Falhas de consolidação no worker |

### Logs (Loki)

Logs estruturados em formato JSON via `ILogger` + OpenTelemetry Log Bridge (OTLP → Collector → Loki).

**Campos emitidos em todos os logs:**

```json
{
  "Timestamp": "2026-06-17T21:00:00.000Z",
  "LogLevel": "Information",
  "Category": "CashFlow.LaunchService.Api.Services.LaunchAppService",
  "Message": "[business] Launch registered. Id=... Date=... Type=... Amount=...",
  "EventId": 0
}
```

**Prefixos de contexto usados no código:**
- `[business]` — operações de domínio (lançamento registrado, saldo atualizado)
- `[application]` — operações de aplicação (cache hit, query, evento publicado)

**Níveis de log por cenário:**

| Nível | Quando |
|---|---|
| `Debug` | Cache hit, queries internas |
| `Information` | Operações bem-sucedidas (lançamento registrado, saldo atualizado) |
| `Warning` | Falha de autenticação, retry de mensagem |
| `Error` | Exceção não tratada, falha de consolidação |

> **Atenção:** dados financeiros (valores) são passados como parâmetros estruturados de log — avalie mascaramento em produção caso seja necessário.

---

### Traces Distribuídos (implementado)

`WithTracing()` configurado nos serviços com `AddAspNetCoreInstrumentation()` e `AddHttpClientInstrumentation()`. Pipeline de traces no Collector exporta para **Grafana Tempo** via OTLP.

**Traces gerados automaticamente:**
- Cada requisição HTTP recebida gera um span raiz (`http.server.*`)
- Chamadas HTTP de saída (BFF → microserviços) geram spans filho (`http.client.*`)
- O `trace_id` correlaciona spans entre serviços para rastreabilidade ponta a ponta

**Como visualizar:** Grafana → Explore → datasource Tempo → pesquisar por `service.name`.

---

## O Que Não Foi Implementado (Evolução Futura)

### Histogramas de latência por operação de negócio

Os histogramas automáticos do ASP.NET Core cobrem latência HTTP geral. Histogramas por operação de negócio específica (ex.: `launch_registration_duration_ms`) não foram criados. Extensão direta do `CashFlowMeters.cs`.

### Profundidade da fila RabbitMQ

A métrica `rabbitmq_queue_depth` não está instrumentada. Pode ser coletada via plugin RabbitMQ Prometheus (`/metrics` nativo do RabbitMQ 3.8+) ou via scrape da Management API.

### Alertas Prometheus

Não há `PrometheusRule` ou arquivo de alertas configurado. O Prometheus do MVP apenas armazena métricas — alertas requerem `alertmanager` e regras definidas.

---

## SLIs e SLOs (referência arquitetural)

### Launch Service

| SLI | Métrica | SLO |
|---|---|---|
| Disponibilidade | `% requisições com HTTP 2xx ou 4xx / total` | ≥ 99,9% |
| Latência P95 | `http_server_request_duration_seconds` p95 | < 200ms |
| Taxa de erro | `% requisições com HTTP 5xx / total` | < 0,1% |

### Daily Balance Service

| SLI | Métrica | SLO |
|---|---|---|
| Disponibilidade | `% requisições com HTTP 2xx ou 4xx / total` | ≥ 99,5% |
| Latência P95 | `http_server_request_duration_seconds` p95 | < 300ms |
| Throughput | Requisições por segundo | ≥ 47,5 req/s (95% de 50) |

### Daily Balance Worker

| SLI | Métrica | SLO |
|---|---|---|
| Taxa de falha | `cashflow_worker_consolidation_errors_total / cashflow_worker_consolidations_total` | < 1% |
| Lag de consolidação | Tempo entre publicação do evento e atualização do saldo | < 30s (observação manual) |

---

## Dashboard Grafana (MVP)

O dashboard **CashFlow — Overview** é provisionado automaticamente em `http://localhost:3001` (user: `admin` / pass: `cashflow123`), folder **CashFlow**.

**Painéis disponíveis:**
- HTTP req/s por serviço (série temporal)
- Latência HTTP p95 por serviço (série temporal)
- Lançamentos registrados na última hora (stat)
- Consultas de saldo na última hora (stat)
- Consolidações do worker na última hora (stat)
- Erros de validação + consolidação (stat — vermelho se > 0)
- Métricas de negócio em rate (série temporal)
- Logs da aplicação via Loki (painel de logs)

**Dashboards planejados mas não criados (evolução futura):**
- Fila RabbitMQ (profundidade + taxa de processamento)
- Infraestrutura Kubernetes (CPU/memória por pod, réplicas HPA/KEDA)
- SLO burn rate e alertas

---

## Retenção de Logs (MVP)

Loki configurado com `retention_period: 168h` (**7 dias**). Para produção, configurar por camada:

| Camada | Período | Storage |
|---|---|---|
| Hot | 7 dias | Disco local (Loki) |
| Warm | 30 dias | Object storage (S3/GCS) |
| Cold / auditoria | 90 dias | S3 Glacier / Cloud Archive |
