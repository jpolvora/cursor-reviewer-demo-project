# fix-pr

Skill autocontida para tratar threads ativas de code review em Pull Requests do GitHub ou Azure DevOps com **julgamento crítico** — não aceita automaticamente todo comentário de bot como bug real.

**Fonte de verdade para o agente:** [`SKILL.md`](SKILL.md)

## Objetivo

`fix-pr` coordena uma rodada completa de correção de PR:

1. Coleta PR, threads, comentários e issues/Work Items via helper padronizado.
2. Analisa cada thread ativa: faz sentido? chance real do erro? coerência com issue/Work Item/plano/código?
3. Classifica com score de urgência 0–10 e propõe ação (`Corrigir em código`, `Resolver com comentário`, `Escalar`).
4. **Para no gate** e pede confirmação do usuário antes de qualquer ação.
5. Corrige apenas o necessário (fix à prova da próxima rodada da pipeline), fecha threads com comentários explicativos, gera report e faz commit/push cirúrgico.

## Dependências permitidas

A skill não depende de outras skills. Use apenas:

| Recurso | Caminho |
|---------|---------|
| Helper Python | `scripts/fix_pr_context.py` |
| Roteamento e regras de agentes | `AGENTS.md` |

**Não use:** REST inline, `curl`, `Invoke-RestMethod` manual ou outras chamadas diretas às APIs de pull request.

## Arquivos e diretórios

### Temporários (não commitar)

```text
.agents/skills/fix-pr/runs/pr-XXX/
├── context.json              # coleta GitHub ou Azure DevOps
├── plan-gate.md              # triagem + gate (pré-confirmação)
├── plan-exec.md              # plano de execução (pós-gate, extensão da skill)
└── thread-YYYY.state.md      # estado por subagent (YYYY = id da thread)
```

A pasta `runs/` tem `.gitignore`. O helper ignora cache Python em `scripts/.gitignore` (`__pycache__/`, `*.pyc`).

### Commitável (quando houver fix em código)

```text
.cursor/codereviews/PR-XXX-rodada-N.md
```

Exemplo: `.cursor/codereviews/PR-12-rodada-3.md`

## Modo dry-run

Ative quando o usuário pedir simulação/review local:

- Coleta, triagem e gate **iguais** ao fluxo normal.
- `resolve-thread --dry-run` (não altera o remote).
- Correções locais e testes quando fizer sentido.
- Report gerado localmente, **sem** stage/commit/push.

## Fluxo (resumo)

```
0. Coleta → 1. Contexto → 2. Julgamento → 3. GATE ⏸ → 3.1. plan-exec.md → confirma execução → 4–6. Execução
```

| Fase | Artefato |
|------|----------|
| Análise + gate | `plan-gate.md` |
| Plano de execução | `plan-exec.md` |
| Execução | etapas 4–6 conforme `plan-exec.md` |

### 0. Coleta

```bash
python .agents/skills/fix-pr/scripts/fix_pr_context.py collect \
  --pr-id XXX \
  --output .agents/skills/fix-pr/runs/pr-XXX/context.json
```

Retorna: `pullRequest`, `workItems[]` (ou `issues[]`), `threads[]`, `activeThreads[]`.

### 1. Montar contexto

Por thread: comentário, arquivo/linha, issue/Work Item (descrição + AC), plano em `.cursor/plans/`, código atual, Cursor rules.

### 2. Julgar cada thread

Perguntas-chave:

- Caminho executável real?
- Chance do erro (alta/média/baixa)?
- Coerência com issue/Work Item e plano?
- Cenário já bloqueado por validator/teste/invariant?
- Impacto material (segurança, dados, regras vitais)?
- Correção proporcional?

**Classificação:**

| Ação | Quando |
|------|--------|
| Resolver com comentário | Falso positivo, nível baixo (≤5), cenário coberto, sem base no WI/plano |
| Corrigir em código | Bug provável, alta urgência (>5), impacto material, falta guarda/teste |
| Escalar | Conflito issue/plano/teste ou decisão de produto/domínio |

**Score 0–10:**

| Score | Urgência |
|-------|----------|
| 0–5 | Nível baixo |
| 6–10 | Alta |

Referência: 0–2 nit/estilo · 3–5 risco baixo · 6–8 bug provável/regressão · 9–10 crítico (segurança/dados).

### 3. Gate de confirmação (obrigatório)

**Pare** antes de `resolve-thread`, edição, subagent, commit ou push.

Monte tabela com colunas: `Thread`, `Arquivo`, `Ação proposta`, `Score`, `Urgência`, `Justificativa`. Separe por:

- Corrigir em código
- Resolver com comentário
- Escalar

Grave em `runs/pr-XXX/plan-gate.md` e pergunte:

```text
Deseja efetuar as correções ou finalização das threads [ID1, ID2]?
```

O usuário pode revisar, discordar, trocar de modelo LLM e confirmar. Sem confirmação → não prossiga.

Se o usuário **recusar** a execução, pergunte se deseja limpar os arquivos temporários em `runs/pr-XXX/`.

### 3.1. Plano de execução (pós-gate)

**Após confirmação do gate**, não execute etapas 4–6 imediatamente:

1. Gere `runs/pr-XXX/plan-exec.md` — extensão da skill com escopo aprovado.
2. Informe o caminho; o usuário pode revisar e trocar de LLM.
3. Pergunte: `Deseja executar o plano normalmente?`
4. Só prossiga para etapas 4–6 se o usuário confirmar.
5. Se recusar executar agora, ofereça limpeza dos temporários em `runs/pr-XXX/`.

O `plan-exec.md` deve conter por thread: ação, estratégia, arquivos permitidos, subagent, testes, rascunho do comentário `resolve-thread`, checklist pipeline, **esqueleto do report commitável** (`.cursor/codereviews/PR-XXX-rodada-N.md`) e checklist de publicação (report + fixes no mesmo commit).

Fluxo preferido: **gate → `plan-exec.md` → confirmação de execução → etapas 4–6**.

### 4. Resolver sem código

Somente após gate **e** confirmação para executar o `plan-exec.md`. Comente e feche com justificativa auditável:

```bash
python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread \
  --pr-id XXX --thread-id YYYY --comment "Sem alteração de código: ..."
```

### 5. Corrigir em código — fix à prova de pipeline

Somente após gate **e** confirmação para executar o `plan-exec.md`. Por thread aprovada:

1. Estratégia cobrindo issue atual **e** críticas previstas na próxima rodada da pipeline.
2. Percorrer caminhos adjacentes: chamadores, validações, DTOs, testes, fluxos UI/API.
3. Subagent dedicado por thread (paralelo se independentes).
4. Teste anti-regressão ou justificativa técnica.
5. Fix deve resistir a novos reviews da pipeline.

### 6. Validar, reportar e fechar threads

1. Build/testes proporcionais.
2. Revisar diff como próxima rodada da pipeline.
3. Se houve fix em código → criar `.cursor/codereviews/PR-XXX-rodada-N.md`:
   - PR, rodada, decisão por thread.
   - Por fix: problema, o que/como foi corrigido, caminhos analisados, testes, riscos residuais.
   - Por resolução sem código: justificativa.
   - Testes executados e arquivos alterados.
4. Fechar threads com comentário explicativo via `resolve-thread` (o que foi feito, como, testes, link do report).

### 7. Commit e push

- `git status` → stage **somente** arquivos do fix + report `.cursor/codereviews/PR-XXX-rodada-N.md`.
- Nunca `git add .` nem `git commit -a`.
- Rodada: `git log --oneline --grep="Fix PR XXX: thread(s):"` → N = commits + 1.

```text
Fix PR XXX: thread(s): [thread1, thread2, threadN] (rodada N)
```

Exemplo: `Fix PR 12: thread(s): [thread_abc123] (rodada 3)`

## Helper Python

### `collect`

```bash
python .agents/skills/fix-pr/scripts/fix_pr_context.py collect \
  --pr-id 1 --output .agents/skills/fix-pr/runs/pr-1/context.json
```

Opções: `--repo-root`, `--repository`, `--platform`.

### `resolve-thread`

```bash
python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread \
  --pr-id 1 --thread-id "PRRT_..." --comment "Justificativa..."
```

Simulação: adicione `--dry-run`.
