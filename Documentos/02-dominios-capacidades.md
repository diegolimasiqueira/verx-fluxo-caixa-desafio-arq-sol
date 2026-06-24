# Domínios Funcionais e Capacidades de Negócio

## Decomposição de Domínios

A plataforma é organizada em dois domínios funcionais principais, seguindo os princípios de **Domain-Driven Design (DDD)** e **Bounded Context**:

---

## Domínio 1 — Lançamentos (Launch)

**Responsabilidade:** Registro, validação e consulta de lançamentos financeiros (créditos e débitos).

### Capacidades de Negócio

| Capacidade | Descrição |
|---|---|
| Registrar Lançamento | Aceita e persiste um lançamento de débito ou crédito com validações de negócio |
| Consultar Lançamentos | Retorna lançamentos por data ou período |
| Publicar Evento | Notifica outros domínios sobre novos lançamentos de forma assíncrona |

### Componentes

- **Launch Service (.NET 10):** API REST com lógica de validação e persistência
- **PostgreSQL Launch:** banco transacional, schema do domínio de lançamentos
- **RabbitMQ:** publicação do evento `LaunchRegistered` após cada gravação bem-sucedida

### Agregados e Entidades

```
Lançamento (Aggregate Root)
  ├── Id (UUID)
  ├── Data (DateOnly)
  ├── Valor (decimal, > 0)
  ├── Tipo (Débito | Crédito)
  ├── Descrição (string)
  └── CriadoEm (DateTime UTC)
```

### Eventos de Domínio

| Evento | Payload | Publicado quando |
|---|---|---|
| `LaunchRegistered` | `{ id, data, valor, tipo, criadoEm }` | Lançamento persistido com sucesso |

---

## Domínio 2 — Saldo Diário Consolidado (Daily Balance)

**Responsabilidade:** Manutenção e consulta do saldo diário consolidado, atualizado de forma assíncrona a partir dos eventos do domínio de lançamentos.

### Capacidades de Negócio

| Capacidade | Descrição |
|---|---|
| Consolidar Saldo Diário | Processa eventos de lançamento e atualiza o saldo do dia correspondente |
| Consultar Saldo por Data | Retorna o saldo consolidado de uma data específica |
| Consultar Histórico de Saldos | Retorna saldos consolidados de um período |

### Componentes

- **Daily Balance Worker (.NET 10 Worker Service):** consumidor de eventos, aplica a lógica de consolidação
- **Daily Balance Service (.NET 10):** API REST somente-leitura para consulta de saldos
- **PostgreSQL Balance:** banco otimizado para leitura, schema do domínio de saldo

### Agregados e Entidades

```
SaldoDiário (Aggregate Root)
  ├── Data (DateOnly) — chave de negócio
  ├── TotalCreditos (decimal)
  ├── TotalDebitos (decimal)
  ├── SaldoConsolidado (decimal) = TotalCreditos - TotalDebitos
  └── AtualizadoEm (DateTime UTC)
```

---

## Mapa de Capacidades de Negócio

```
Cash Flow Platform
├── Gestão de Lançamentos
│   ├── Registrar lançamento de crédito
│   ├── Registrar lançamento de débito
│   └── Consultar histórico de lançamentos
└── Gestão de Saldo
    ├── Consolidar saldo diário (assíncrono)
    ├── Consultar saldo do dia
    └── Consultar histórico de saldos
```

---

## Padrão de Integração entre Domínios

Os dois domínios se comunicam **exclusivamente via eventos assíncronos**, respeitando os limites do Bounded Context:

```
[Launch Domain]                    [Daily Balance Domain]
     |                                      |
     |-- LaunchRegistered (RabbitMQ) -----> |
     |                                      |-- atualiza SaldoDiário
     |                                      |
```

Esta separação garante que:
1. Uma falha no domínio de consolidação **não afeta** o registro de lançamentos
2. Cada domínio evolui de forma independente
3. O contrato entre domínios é o evento publicado no broker — qualquer consumidor pode ser adicionado sem alterar o produtor

---

## Arquitetura de Canal (BFF)

O **BFF (Backend for Frontend)** é o ponto único de entrada do canal web, sem conter lógica de negócio. Suas responsabilidades:

- Roteamento (proxy) de requisições para o microserviço correto
- Autenticação (emissão de JWT) e autorização (RBAC por role)
- Rate limiting e throttling por cliente/IP
- Security headers centralizados
- Gerenciamento de usuários (domínio de IAM, banco `bff_db`)

> **Evolução futura:** composição de respostas (ex: lançamentos + saldo do dia em uma única chamada) — requer aggregation layer no BFF.
