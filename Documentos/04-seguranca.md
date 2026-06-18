# Segurança

## Modelo de Ameaças

Os principais vetores de ameaça para a plataforma são:

| Ameaça | Superfície | Mitigação |
|---|---|---|
| Acesso não autenticado | Canal externo (HTTPS) | JWT obrigatório no BFF |
| Interceptação de tráfego interno | Comunicação entre pods | mTLS via Istio |
| Escalonamento de privilégios | API de lançamentos | RBAC no BFF |
| Injeção de dados maliciosos | Payload de lançamentos | Validação e sanitização no serviço |
| DDoS / abuso de API | BFF público | Rate limiting + throttling |
| Comprometimento do broker | RabbitMQ | Autenticação com credenciais rotacionadas + VNet isolada |
| Exposição de dados sensíveis | Logs e traces | Mascaramento de dados financeiros em logs |

---

## Canal Externo (BFF → Frontend)

### Autenticação
- Todos os endpoints do BFF exigem **JWT Bearer Token** (OAuth2/OIDC)
- O token é validado no BFF antes de qualquer roteamento downstream
- Emissor (Identity Provider) deve ser configurável via `OIDC_AUTHORITY` environment variable
- Tokens com validade curta (≤ 1h) com refresh token rotativo

### Autorização
- RBAC implementado no BFF com roles extraídas do JWT (`claims`)
- Role `merchant`: acesso a lançamentos e saldo próprio
- Role `admin`: acesso a lançamentos e saldos de qualquer comerciante (evolução futura)

### Proteção de Transporte
- HTTPS obrigatório via TLS 1.2+ no Ingress Controller (NGINX)
- HSTS habilitado com `max-age` de 1 ano
- Headers de segurança: `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`

### Rate Limiting
- Limite de requisições por IP: **100 req/min** (configurável)
- Limite de requisições por usuário autenticado: **200 req/min**
- Retorna HTTP 429 com header `Retry-After`

---

## Canal Interno (Service-to-Service)

### mTLS via Istio
- Toda comunicação entre pods no cluster é criptografada com **mTLS automático** (Istio PeerAuthentication)
- Certificados gerenciados pelo Istio CA (Citadel) com rotação automática
- Modo: `STRICT` — nenhuma comunicação em plaintext é aceita entre serviços

### Network Policies
- Políticas de rede Kubernetes restringem comunicação entre namespaces
- Launch Service só aceita conexões do BFF
- Daily Balance Service só aceita conexões do BFF
- Daily Balance Worker só aceita conexões do RabbitMQ (consumer)
- Bancos de dados não são acessíveis fora do namespace do domínio

---

## Broker de Mensagens (RabbitMQ)

- Autenticação com usuário e senha por serviço (credenciais individuais por produtor/consumidor)
- Virtual Hosts separados por domínio (`/launch`, `/balance`)
- TLS habilitado na porta de comunicação do broker
- Credenciais armazenadas em **Kubernetes Secrets** (preferencialmente gerenciadas por Vault em produção)
- Permissões de publish restritas ao Launch Service; permissões de consume restritas ao Worker

---

## Banco de Dados

- Credenciais armazenadas em **Kubernetes Secrets** ou **Vault**
- Usuários de banco com permissões mínimas:
  - Launch Service: `INSERT`, `SELECT` em tabela de lançamentos
  - Daily Balance Worker: `INSERT`, `UPDATE` em tabela de saldo
  - Daily Balance Service: `SELECT` em tabela de saldo
- Criptografia em repouso habilitada no volume do PostgreSQL
- Backups automáticos diários com retenção de 30 dias

---

## Gestão de Secrets

| Ambiente | Mecanismo |
|---|---|
| Desenvolvimento local | `dotnet user-secrets` / `.env` (não versionado) |
| Kubernetes | `Kubernetes Secrets` com RBAC restrito |
| Produção | **HashiCorp Vault** com injeção via sidecar ou CSI Driver |

---

## Auditoria e Rastreabilidade

- Todos os registros de lançamentos incluem `CriadoEm` (UTC) e `UsuárioId` (do JWT)
- Logs de autenticação (sucesso/falha) emitidos no BFF com nível `Warning` para falhas
- Traces distribuídos (OpenTelemetry) permitem rastrear uma requisição de ponta a ponta
- Retenção de logs de auditoria: mínimo 90 dias

---

## Checklist de Segurança por Camada

```
[ Canal Externo ]
  ✓ HTTPS/TLS 1.2+
  ✓ JWT Bearer obrigatório
  ✓ RBAC por role
  ✓ Rate limiting
  ✓ Headers de segurança

[ Comunicação Interna ]
  ✓ mTLS (Istio STRICT mode)
  ✓ Network Policies (Kubernetes)
  ✓ Zero trust entre namespaces

[ Dados ]
  ✓ Credenciais em Secrets / Vault
  ✓ Permissões mínimas por serviço
  ✓ Criptografia em repouso
  ✓ Backup automático

[ Operação ]
  ✓ Logs de auditoria
  ✓ Traces distribuídos
  ✓ Alertas de falhas de autenticação
```
