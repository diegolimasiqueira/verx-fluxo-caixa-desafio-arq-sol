# Requisitos Funcionais e Não-Funcionais

## Contexto de Negócio

Um comerciante precisa controlar seu fluxo de caixa diário com registros de débitos e créditos, além de consultar o saldo diário consolidado.

---

## Requisitos Funcionais

### RF-01 — Registro de Lançamentos
- O sistema deve permitir registrar lançamentos financeiros do tipo **débito** ou **crédito**
- Cada lançamento deve conter: data, valor, tipo (débito/crédito) e descrição
- Lançamentos registrados não podem ser excluídos (imutabilidade do ledger)
- O sistema deve validar que o valor é positivo e o tipo é válido

### RF-02 — Consulta de Lançamentos
- O sistema deve permitir listar lançamentos por data ou intervalo de datas
- O sistema deve retornar lançamentos ordenados por data/hora de registro

### RF-03 — Consolidado Diário
- O sistema deve calcular e disponibilizar o saldo consolidado por dia
- O saldo consolidado = soma dos créditos − soma dos débitos do dia
- O consolidado deve refletir todos os lançamentos registrados até o momento da consulta

### RF-04 — Interface Web
- O comerciante acessa a plataforma via navegador
- A interface permite registrar lançamentos e consultar o saldo diário

---

## Requisitos Não-Funcionais

### RNF-01 — Disponibilidade e Isolamento de Falhas
- O serviço de controle de lançamentos **não deve ficar indisponível** caso o serviço de consolidado diário fique fora do ar
- Os domínios são desacoplados via mensageria assíncrona (RabbitMQ)
- **Meta:** disponibilidade do Launch Service ≥ 99,9% independente do Daily Balance Service

### RNF-02 — Desempenho e Capacidade
- Em dias de pico, o serviço de consolidado diário deve suportar **50 requisições por segundo**
- Taxa máxima de perda de requisições: **5%**
- Latência alvo para consulta de saldo consolidado (P95): < 300ms
- Latência alvo para registro de lançamento (P95): < 200ms

### RNF-03 — Escalabilidade
- Os serviços devem escalar horizontalmente sem alteração de código
- O Daily Balance Worker deve escalar automaticamente com base na profundidade da fila (KEDA)
- O Daily Balance Service deve escalar com base em CPU/memória (HPA)

### RNF-04 — Consistência
- O registro de lançamentos é **fortemente consistente** (gravação síncrona no banco transacional)
- O consolidado diário é **eventualmente consistente** — atualizado de forma assíncrona após cada lançamento registrado
- O lag de consolidação deve ser monitorado e alertado se ultrapassar 30 segundos

### RNF-05 — Segurança

**MVP (implementado):**
- Comunicações externas via HTTPS/TLS (Ingress em produção)
- Autenticação via **JWT emitido pelo BFF** após login e-mail/senha (usuários em `bff_db`, senhas com hash PBKDF2)
- Autorização baseada em roles no BFF (`admin`, `merchant`)
- Rate limiting e security headers no BFF
- Microserviços validam JWT repassado pelo BFF (sem login próprio)

**Evolução futura** (não faz parte do MVP — ver `07-evolucoes-futuras.md`):
- mTLS entre serviços (Istio)
- Identity Provider externo (ex.: Keycloak/OIDC)

### RNF-06 — Observabilidade
- Todos os serviços expõem métricas, logs estruturados e traces distribuídos
- Stack: OpenTelemetry → Prometheus + Loki → Grafana
- SLIs e SLOs definidos para cada serviço crítico

### RNF-07 — Resiliência
- Circuit breaker nas chamadas entre serviços (via Istio)
- Retry com backoff exponencial para publicação de eventos no broker
- Dead Letter Queue (DLQ) para mensagens que falham repetidamente no worker

### RNF-08 — Manutenibilidade
- Código organizado por domínio, seguindo princípios de Clean Architecture
- Migrações de banco versionadas (Entity Framework Migrations)
- Contratos de API documentados via OpenAPI/Swagger
