# Verx — Desafio Arquiteto de Soluções: Cash Flow Platform

Solução para controle de fluxo de caixa diário com registro de lançamentos (débitos e créditos) e consolidação de saldo diário.

---

## Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) + Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (para build do frontend)

---

## Como Rodar Localmente

> **Importante:** use sempre o `./build.sh`, não o `docker compose up` diretamente.
> Os Dockerfiles copiam o output de `dotnet publish` gerado localmente — sem rodar o script, as imagens falham no build por não encontrar o diretório `publish/`.

```bash
git clone <repo-url>
cd verx-fluxo-caixa-desafio-arq-sol
chmod +x build.sh
./build.sh
```

O script:
1. Executa `dotnet publish` para cada serviço (usa o cache NuGet local, output em `build-output/`)
2. Executa `npm install && npm run build` no frontend
3. Executa `docker compose up --build` (os containers copiam os artefatos já gerados)

> **Primeira execução:** o Docker pode precisar baixar as imagens base (`postgres`, `rabbitmq`, `nginx`, `dotnet/aspnet`). Se houver erro de DNS no pull de imagens, baixe-as manualmente com `docker pull nginx:alpine` antes de rodar o script.

Aguarde todos os containers ficarem `healthy`/`Up`. Em seguida acesse:

| Serviço | URL | Descrição |
|---|---|---|
| **Frontend** | http://localhost:3000 | Interface web (login: `admin` / `admin`) |
| **Launch Service** | http://localhost:5001/swagger | Registro e consulta de lançamentos |
| **Daily Balance Service** | http://localhost:5002/swagger | Consulta de saldo consolidado |
| **RabbitMQ Management** | http://localhost:15672 | user: `guest` / pass: `guest` |

---

## Fluxo de Teste (passo a passo)

### Via Interface Web (recomendado)

1. Acesse http://localhost:3000
2. Faça login com `admin` / `admin`
3. Use o menu lateral para navegar entre **Lançamentos** e **Saldo Diário**

### Via Swagger (API direta)

No Swagger do **Launch Service** (`http://localhost:5001/swagger`):

- `POST /api/auth/login`
```json
{ "username": "admin", "password": "admin" }
```
- Copie o `accessToken` retornado
- Clique em **Authorize** (canto superior direito) e cole o token

> O mesmo token funciona em ambos os serviços (chave JWT compartilhada).

### 2. Registrar lançamentos

- `POST /api/launches`
```json
{ "date": "2026-06-17", "amount": 1000.00, "type": "credit", "description": "Venda do dia" }
```
```json
{ "date": "2026-06-17", "amount": 300.00, "type": "debit", "description": "Pagamento fornecedor" }
```

### 3. Consultar saldo consolidado

No Swagger do **Daily Balance Service** (`http://localhost:5002/swagger`):

- Autorize com o mesmo token (passos 1)
- `GET /api/balance/2026-06-17`

Resultado esperado:
```json
{
  "date": "2026-06-17",
  "totalCredits": 1000.00,
  "totalDebits": 300.00,
  "consolidatedBalance": 700.00
}
```

> O saldo é consolidado **assincronamente** via RabbitMQ pelo `daily-balance-worker`. Aguarde 2–5 segundos após registrar o lançamento antes de consultar.

---

## Rodar os Testes

```bash
dotnet test
```

Resultado esperado:

```
Aprovado! – Com falha: 0, Aprovado: 10, Total: 10 - LaunchService.Tests
Aprovado! – Com falha: 0, Aprovado:  4, Total:  4 - DailyBalanceWorker.Tests
Aprovado! – Com falha: 0, Aprovado:  4, Total:  4 - DailyBalanceService.Tests
```

**18 testes no total — todos passando.**

Cobertura:
- Domínio `Launch`: validações de amount, description, tipo, trim
- `LaunchRegisteredConsumer`: crédito, débito, acumulação múltipla, idempotência de registro existente
- `DailyBalanceQueryService`: saldo por data, not found, período ordenado, período vazio

---

## Parar o Ambiente

```bash
docker compose down
```

Para remover também os volumes (banco de dados):
```bash
docker compose down -v
```

---

## Estrutura do Repositório

```
.
├── build.sh                          # Script de build + compose (use este para subir)
├── docker-compose.yml
├── CashFlow.slnx                     # Solution .NET 10
├── frontend/
│   ├── src/
│   │   ├── contexts/                 # AuthContext (JWT)
│   │   ├── pages/                    # LoginPage, DashboardPage, LaunchesPage, BalancePage
│   │   ├── components/layout/        # AppLayout (sidebar + header)
│   │   ├── services/                 # api.ts, auth.ts, launches.ts, balance.ts
│   │   └── types/                    # Tipos TypeScript compartilhados
│   ├── Dockerfile                    # Nginx servindo o build estático
│   └── nginx.conf
├── infra/
│   └── postgres/
│       └── init.sql                  # Cria os bancos launch_db e daily_balance_db
├── src/
│   ├── LaunchService/
│   │   ├── CashFlow.LaunchService.Api/
│   │   │   ├── Controllers/          # AuthController, LaunchesController
│   │   │   ├── Domain/               # Launch (aggregate), LaunchType, LaunchRegisteredEvent
│   │   │   ├── Data/                 # LaunchDbContext + Migrations EF Core
│   │   │   ├── Services/             # LaunchAppService, TokenService
│   │   │   ├── Middleware/           # GlobalExceptionMiddleware
│   │   │   └── DTOs/
│   │   └── CashFlow.LaunchService.Tests/
│   ├── DailyBalanceService/
│   │   ├── CashFlow.DailyBalanceService.Api/
│   │   │   ├── Controllers/          # AuthController, DailyBalanceController
│   │   │   ├── Domain/               # DailyBalance
│   │   │   ├── Data/                 # BalanceDbContext (schema gerenciado pelo Worker)
│   │   │   ├── Services/             # DailyBalanceQueryService, TokenService
│   │   │   └── Middleware/           # GlobalExceptionMiddleware
│   │   └── CashFlow.DailyBalanceService.Tests/
│   └── DailyBalanceWorker/
│       ├── CashFlow.DailyBalanceWorker/
│       │   ├── Consumers/            # LaunchRegisteredConsumer (MassTransit 8.x)
│       │   ├── Domain/               # DailyBalance, LaunchRegisteredEvent
│       │   └── Data/                 # WorkerDbContext (dono do schema daily_balance_db)
│       └── CashFlow.DailyBalanceWorker.Tests/
└── diagramas/
    └── c4/
        ├── model.c4                  # Diagrama C4 (Context, Container, Deployment)
        └── spec.c4
```

---

## Arquitetura

```
[Comerciante]
    |
    v (HTTPS + JWT)
[Launch Service :5001]  ---> [PostgreSQL launch_db]
    |
    | evento LaunchRegistered (RabbitMQ fanout exchange)
    v
[RabbitMQ]
    |
    v (consumer assíncrono — retry 5s/15s/30s)
[Daily Balance Worker]  ---> [PostgreSQL daily_balance_db]
                                       ^
                                       | (somente leitura)
              [Daily Balance Service :5002]
```

### Decisões Chave

| Decisão | Escolha | Motivo |
|---|---|---|
| Padrão | Microserviços + Event-Driven | Isolamento de falhas exigido pelo requisito não-funcional |
| Bancos | PostgreSQL separados por domínio | CQRS — escrita e leitura desacopladas |
| Broker | RabbitMQ 8.x (MassTransit) | DLQ nativa, retry automático, sem overhead de Kafka |
| Runtime | .NET 10 / ASP.NET Core | Performance, LTS, ecossistema maduro |
| Segurança | JWT Bearer (mock) | Stateless — em produção: Keycloak como IdP |
| Schema | Worker é o dono do `daily_balance_db` | Evita conflito de migrations entre serviços |

---

## Visualizar Diagramas C4

Os diagramas estão no formato **LikeC4 DSL** (arquivos `.c4`).

1. Instale a extensão [**LikeC4**](https://marketplace.visualstudio.com/items?itemName=likec4.likec4) no VS Code
2. Abra o arquivo `diagramas/c4/model.c4`
3. Clique em **Preview** no canto superior direito do editor

Três views disponíveis: **Context** (nível 1), **Container** (nível 2) e **Deployment — Kubernetes Runtime**.

---

## Documentação

| Documento | Conteúdo |
|---|---|
| [01-requisitos.md](Documentos/01-requisitos.md) | Requisitos funcionais e não-funcionais refinados |
| [02-dominios-capacidades.md](Documentos/02-dominios-capacidades.md) | Mapeamento de domínios e capacidades de negócio |
| [03-decisoes-arquiteturais.md](Documentos/03-decisoes-arquiteturais.md) | ADRs com justificativas de tecnologia e padrões |
| [04-seguranca.md](Documentos/04-seguranca.md) | Critérios de segurança para consumo de serviços |
| [05-observabilidade.md](Documentos/05-observabilidade.md) | Estratégia de monitoramento e observabilidade |
| [06-estimativa-custos.md](Documentos/06-estimativa-custos.md) | Estimativa referencial de infraestrutura (AWS/GCP/on-prem) |
| [07-evolucoes-futuras.md](Documentos/07-evolucoes-futuras.md) | Roadmap técnico e evoluções planejadas |
