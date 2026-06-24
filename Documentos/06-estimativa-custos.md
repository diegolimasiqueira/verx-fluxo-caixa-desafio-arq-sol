# Estimativa de Custos de Infraestrutura

> Estimativa referencial baseada em preços AWS (região `us-east-1`) e GCP (`us-central1`) para um ambiente de produção com carga de pico de 50 req/s no Daily Balance Service. Valores em USD/mês.

---

## Topologia de Produção (Baseline)

| Componente | Réplicas (baseline) | Réplicas (pico) |
|---|---|---|
| BFF / API Gateway | 2 | 4 |
| Launch Service | 2 | 4 |
| Daily Balance Service | 2 | 6 |
| Daily Balance Worker | 2 | 8 (KEDA) |
| RabbitMQ | 3 (cluster) | 3 |
| PostgreSQL Launch | 1 primary + 1 replica | — |
| PostgreSQL Balance | 1 primary + 1 replica | — |

---

## Estimativa AWS (EKS)

### Kubernetes (EKS)

| Item | Especificação | Custo/mês |
|---|---|---|
| EKS Control Plane | — | $73 |
| Nós Worker (baseline) | 3x `m6i.xlarge` (4 vCPU, 16 GB) | $420 |
| Nós Worker (pico, spot) | +3x `m6i.xlarge` Spot (~70% desconto) | $127 |
| NAT Gateway | 2x AZ | $65 |
| Load Balancer (NLB) | 1x | $18 |
| **Subtotal Compute** | | **~$703** |

### Banco de Dados (RDS PostgreSQL)

| Item | Especificação | Custo/mês |
|---|---|---|
| PostgreSQL Launch (primary) | `db.t4g.medium` (2 vCPU, 4 GB) + 100 GB SSD | $75 |
| PostgreSQL Launch (replica) | `db.t4g.medium` | $55 |
| PostgreSQL Balance (primary) | `db.t4g.medium` + 100 GB SSD | $75 |
| PostgreSQL Balance (replica) | `db.t4g.medium` | $55 |
| **Subtotal RDS** | | **~$260** |

### Mensageria

| Item | Especificação | Custo/mês |
|---|---|---|
| Amazon MQ (RabbitMQ) | `mq.m5.large` cluster (3 brokers) | $220 |
| **Subtotal Broker** | | **~$220** |

### Observabilidade

| Item | Especificação | Custo/mês |
|---|---|---|
| Amazon Managed Prometheus | 10 GB métricas/mês | $20 |
| Amazon Managed Grafana | 5 usuários ativos | $25 |
| CloudWatch Logs (Loki alternativo) | 50 GB ingestão/mês | $30 |
| **Subtotal Observabilidade** | | **~$75** |

### Armazenamento e Rede

| Item | Custo/mês |
|---|---|
| EBS Volumes (nós + backup) | $40 |
| Transfer (egress ~100 GB) | $9 |
| **Subtotal** | **~$49** |

### **Total Estimado AWS: ~$1.307/mês**

---

## Estimativa GCP (GKE)

| Item | Especificação | Custo/mês |
|---|---|---|
| GKE Autopilot | Compute equivalente | ~$650 |
| Cloud SQL PostgreSQL | 2x instâncias `db-n1-standard-2` + replicas | $280 |
| Cloud Pub/Sub (alternativa) | — | $15 |
| Managed RabbitMQ (VM + self-managed) | 3x `n2-standard-2` | $190 |
| Cloud Monitoring + Logging | 50 GB logs/mês | $50 |
| **Total Estimado GCP** | | **~$1.185/mês** |

---

## Estimativa Self-Managed (On-Premises / Bare Metal)

Para organizações com datacenter próprio ou VMs dedicadas:

| Item | Custo estimado |
|---|---|
| Hardware / VMs (amortizado) | $300-500/mês |
| PostgreSQL | Open-source (sem licença) |
| RabbitMQ | Open-source (sem licença) |
| Kubernetes (k8s / k3s) | Open-source (sem licença) |
| Grafana / Prometheus / Loki | Open-source (sem licença) |
| Operação (SRE/DevOps) | Custo de pessoal (variável) |
| **Total estimado (infra)** | **~$300-500/mês** |

---

## Licenças de Software

| Software | Modelo | Custo |
|---|---|---|
| .NET 10 | MIT / gratuito | $0 |
| PostgreSQL | PostgreSQL License / gratuito | $0 |
| RabbitMQ | MPL 2.0 / gratuito | $0 |
| Kubernetes | Apache 2.0 / gratuito | $0 |
| Grafana OSS | AGPLv3 / gratuito | $0 |
| Prometheus | Apache 2.0 / gratuito | $0 |
| Loki | AGPLv3 / gratuito | $0 |
| OpenTelemetry | Apache 2.0 / gratuito | $0 |
| Istio | Apache 2.0 / gratuito | $0 |
| **Total licenças** | | **$0** |

> A stack escolhida é 100% open-source, sem custo de licença de software.

---

## Otimizações de Custo

1. **Spot/Preemptible Instances** para workers stateless: redução de até 70% no custo de compute dos pods do Daily Balance Worker
2. **Reserved Instances (1 ano)** para nós base do cluster: redução de 30-40%
3. **Escalamento a zero** do Daily Balance **Worker** fora do horário comercial (KEDA com `minReplicaCount: 0` — quando a fila está vazia). O Daily Balance **Service** (API de leitura) mantém `minReplicas: 1` para evitar cold start na resposta ao usuário
4. **Retenção de logs** configurada por camada: 7 dias hot, 30 dias warm, 90 dias cold (S3/GCS)

---

## Resumo Comparativo

| Opção | Custo/mês | Complexidade Operacional |
|---|---|---|
| AWS EKS (managed) | ~$1.307 | Baixa |
| GCP GKE (autopilot) | ~$1.185 | Baixa |
| Self-Managed (on-prem) | ~$300-500 | Alta |

> Recomendação: para um time pequeno ou estágio inicial, **GCP GKE Autopilot** oferece o melhor custo-benefício com menor overhead operacional. Para escala maior, avaliar Reserved Instances em AWS.
