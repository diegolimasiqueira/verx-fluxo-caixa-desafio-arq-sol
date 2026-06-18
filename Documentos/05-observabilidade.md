# Monitoramento e Observabilidade

## Stack de Observabilidade

```
[ Serviços / Workers ]
       |
       v
[ OpenTelemetry Collector ]
       |
       ├──> [ Prometheus ]  ──> [ Grafana ]
       └──> [ Loki ]        ──> [ Grafana ]
```

Todos os serviços instrumentam via **OpenTelemetry SDK** (.NET) e enviam para o Collector, que centraliza o roteamento para os backends de métricas e logs.

---

## Os Três Pilares

### 1. Métricas (Prometheus)

Cada serviço expõe o endpoint `/metrics` (Prometheus scrape format) via OpenTelemetry.

**Métricas instrumentadas por serviço:**

| Métrica | Tipo | Descrição |
|---|---|---|
| `launch_registrations_total` | Counter | Total de lançamentos registrados |
| `launch_registration_duration_ms` | Histogram | Latência de registro de lançamento |
| `launch_validation_errors_total` | Counter | Erros de validação de lançamento |
| `daily_balance_queries_total` | Counter | Total de consultas de saldo |
| `daily_balance_query_duration_ms` | Histogram | Latência de consulta de saldo |
| `rabbitmq_queue_depth` | Gauge | Profundidade da fila `launch.registered` |
| `worker_consolidation_duration_ms` | Histogram | Latência de consolidação por evento |
| `worker_consolidation_errors_total` | Counter | Falhas de consolidação |
| `worker_dlq_messages_total` | Counter | Mensagens enviadas para DLQ |

### 2. Logs (Loki)

Logs estruturados em formato JSON, emitidos via `ILogger` + OpenTelemetry Log Bridge.

**Campos obrigatórios em todos os logs:**

```json
{
  "timestamp": "2026-06-17T21:00:00Z",
  "level": "Information",
  "service": "launch-service",
  "traceId": "abc123...",
  "spanId": "def456...",
  "message": "Lançamento registrado com sucesso",
  "launchId": "uuid",
  "userId": "uuid"
}
```

**Níveis de log por cenário:**

| Nível | Quando usar |
|---|---|
| `Debug` | Detalhes de execução (apenas dev) |
| `Information` | Operações bem-sucedidas (lançamento registrado, saldo atualizado) |
| `Warning` | Falha de autenticação, retry de mensagem, lag de fila elevado |
| `Error` | Exceção não tratada, falha de persistência, mensagem na DLQ |
| `Critical` | Falha total de componente (banco inacessível, broker fora) |

**Dados financeiros nunca devem aparecer em logs** (valores, descrições sensíveis). Usar mascaramento ou omitir campos.

### 3. Traces Distribuídos (OpenTelemetry → Tempo / Jaeger)

Cada requisição gera um `traceId` propagado por todos os serviços via headers `traceparent` (W3C Trace Context).

**Spans instrumentados:**

- `POST /launches` → span raiz no BFF → span no Launch Service → span na gravação do banco → span na publicação do evento
- `GET /balance/{date}` → span raiz no BFF → span no Daily Balance Service → span na leitura do banco
- Consumo de mensagem no Worker → span de consumo → span de atualização do banco

---

## SLIs e SLOs

### Launch Service

| SLI | Métrica | SLO |
|---|---|---|
| Disponibilidade | `% requisições com HTTP 2xx ou 4xx / total` | ≥ 99,9% |
| Latência P95 | `launch_registration_duration_ms[p95]` | < 200ms |
| Taxa de erro | `% requisições com HTTP 5xx / total` | < 0,1% |

### Daily Balance Service

| SLI | Métrica | SLO |
|---|---|---|
| Disponibilidade | `% requisições com HTTP 2xx ou 4xx / total` | ≥ 99,5% |
| Latência P95 | `daily_balance_query_duration_ms[p95]` | < 300ms |
| Throughput | Requisições por segundo | ≥ 47,5 req/s (95% de 50) |

### Daily Balance Worker

| SLI | Métrica | SLO |
|---|---|---|
| Lag de consolidação | Tempo médio entre publicação do evento e atualização do saldo | < 30s |
| Taxa de falha | `worker_consolidation_errors_total / worker_consolidation_total` | < 1% |
| DLQ | Mensagens na DLQ | = 0 (alerta imediato) |

---

## Dashboards Grafana

### Dashboard: Visão Operacional
- Status de saúde dos serviços (UP/DOWN)
- Throughput atual vs. SLO
- Latência P50/P95/P99 por serviço
- Taxa de erro por serviço

### Dashboard: Fila e Worker
- Profundidade da fila `launch.registered` (tempo real)
- Taxa de processamento do worker (eventos/segundo)
- Lag de consolidação médio
- Réplicas ativas do worker (escalonamento KEDA)

### Dashboard: Infraestrutura Kubernetes
- CPU e memória por pod
- Número de réplicas por deployment
- Eventos de escalonamento (HPA/KEDA)

---

## Alertas

| Alerta | Condição | Severidade | Ação |
|---|---|---|---|
| Launch Service down | `up{service="launch-service"} == 0` | Critical | PagerDuty imediato |
| Alta latência P95 | `p95 > 500ms por 5min` | Warning | Investigar banco/pod |
| DLQ com mensagens | `worker_dlq_messages_total > 0` | Error | Análise de mensagem + reprocessamento |
| Fila crescendo | `rabbitmq_queue_depth > 1000` | Warning | Verificar workers / escalonamento |
| Taxa de erro > 1% | `http_5xx_rate > 0.01` | Error | Rollback / investigação |
| Lag de consolidação > 60s | `consolidation_lag_seconds > 60` | Warning | Verificar worker e broker |
