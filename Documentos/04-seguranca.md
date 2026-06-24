# Segurança

## Modelo de Ameaças

| Ameaça | Superfície | Mitigação |
|---|---|---|
| Acesso não autenticado | Canal externo (HTTP/HTTPS) | JWT obrigatório no BFF |
| Interceptação de tráfego interno | Comunicação entre pods | mTLS via Istio (produção K8s — ver evolução) |
| Escalonamento de privilégios | API de lançamentos e usuários | RBAC no BFF |
| Injeção de dados maliciosos | Payload de lançamentos | Validação e sanitização no serviço |
| DDoS / abuso de API | BFF público | Rate limiting + throttling |
| Comprometimento do broker | RabbitMQ | Credenciais por serviço; VNet isolada em produção |
| Exposição de dados sensíveis | Logs | Dados financeiros como parâmetros estruturados — avaliar mascaramento em produção |

---

## O Que Está Implementado (MVP Local + Docker Compose)

### Autenticação

- Login centralizado no **BFF**: e-mail + senha validados contra a tabela `Users` em `bff_db`
- Senhas armazenadas com hash **PBKDF2-SHA256** (salt por usuário, sem texto plano)
- Após autenticação, o BFF emite **JWT Bearer** com claims de e-mail, nome e role
- Endpoints protegidos exigem token válido antes de rotear para microserviços
- O mesmo token é repassado aos microserviços, que validam assinatura/issuer/audience independentemente
- Usuário padrão criado na subida: `admin@admin.com` / `Master@123` (role `admin`)

> Comunicação local roda em **HTTP** (sem TLS). TLS é responsabilidade do Ingress Controller em produção.

### Autorização (RBAC)

- Role `merchant`: acesso a lançamentos e saldo
- Role `admin`: acesso a lançamentos, saldo e gerenciamento de usuários

### Rate Limiting (BFF)

- Limite no login: **10 req/min** por IP
- Limite nos demais endpoints: **100 req/min** por usuário autenticado (ou IP)
- Retorna HTTP 429 com header `Retry-After: 60`

### Security Headers (BFF)

Middleware `SecurityHeadersMiddleware` adiciona em todas as respostas:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy: default-src 'self'`

### Secrets (MVP Local)

Secrets ficam em variáveis de ambiente no `docker-compose.yml`. Isso é intencional para reduzir atrito na execução local — **não representa prática de produção**.

| Secret | Serviços | Origem no MVP |
|---|---|---|
| `Jwt:Secret` | BFF, Launch, Daily Balance | `docker-compose.yml` |
| Connection strings Postgres | Todos os serviços | `docker-compose.yml` |
| RabbitMQ user/password | Launch, Worker | `docker-compose.yml` |

---

## Checklist por Camada

### MVP (implementado — Docker Compose local)

```
[ Canal Externo — BFF ]
  ✓ JWT Bearer (emitido pelo BFF, validado nos microserviços)
  ✓ Login e-mail/senha com hash PBKDF2-SHA256
  ✓ RBAC por role (admin / merchant)
  ✓ Rate limiting (10/min login, 100/min API)
  ✓ Security headers (X-Frame-Options, X-Content-Type-Options, CSP)

[ Microserviços ]
  ✓ Validação JWT independente por serviço

[ Secrets ]
  ✓ Variáveis de ambiente (valores fixos — apenas local)
  ✗ TLS/HTTPS (HTTP puro no Compose)
```

### Evolução Futura (produção — Kubernetes)

```
[ Canal Externo ]
  → HTTPS/TLS 1.2+ (Ingress Controller com cert-manager)
  → HSTS max-age 1 ano
  → IdP externo (ex.: Keycloak/OIDC)

[ Comunicação Interna ]
  → mTLS automático (Istio STRICT mode)
  → Network Policies Kubernetes por namespace

[ Secrets ]
  → Kubernetes Secrets (staging)
  → HashiCorp Vault ou cloud KMS (produção)

[ Dados ]
  → Usuários de banco com permissões mínimas por operação
  → Criptografia em repouso (volume encryption)
  → Backups automáticos diários com retenção de 30 dias

[ Operação ]
  → Logs de auditoria de autenticação (falhas com Warning)
  → Alertas em falhas repetidas de autenticação
  → Traces distribuídos (rastreabilidade ponta a ponta)
```

---

## Canal Interno — Evolução Produção (Istio)

> Esta seção descreve a arquitetura-alvo de produção, **não implementada no MVP**.

- Toda comunicação entre pods no cluster seria criptografada com **mTLS automático** (Istio PeerAuthentication)
- Certificados gerenciados pelo Istio CA com rotação automática
- Modo: `STRICT` — nenhuma comunicação em plaintext aceita entre serviços
- Network Policies Kubernetes: Launch/Balance só aceitam do BFF; Worker só do RabbitMQ; bancos não expostos fora do namespace

---

## Broker de Mensagens (RabbitMQ)

**MVP:** usuário `guest` / senha `guest` — apenas local, sem exposição externa.

**Produção:**
- Usuário e senha por serviço (produtor e consumidor com permissões separadas)
- Virtual Hosts separados por domínio
- TLS habilitado na porta do broker
- Credenciais em Kubernetes Secrets / Vault

---

## Gestão de Secrets — Evolução Produção

| Ambiente | Mecanismo |
|---|---|
| Local (MVP) | Variáveis de ambiente no `docker-compose.yml` |
| Staging (K8s) | Kubernetes Secrets com RBAC restrito |
| Produção | HashiCorp Vault (sidecar ou CSI Driver) — rotação, auditoria, políticas |

Fluxo em produção: secret criado no Vault → injetado no pod como variável de ambiente → ASP.NET Core lê via `IConfiguration` **sem alteração de código**.
