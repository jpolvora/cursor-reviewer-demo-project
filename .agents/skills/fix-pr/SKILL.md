---
name: fix-pr
description: Analisa criticamente threads de code review em PRs do GitHub ou Azure DevOps, decide se cada comentário faz sentido e corrige apenas o necessário via commit/push cirúrgico.
---

# fix-pr

## Objetivo

Atuar como senior developer (.NET/Angular) para tratar threads ativas de code review em Pull Requests do GitHub ou Azure DevOps. Não aceite todo comentário cegamente: avalie profundamente se cada thread faz sentido técnico, se é coerente com a US/plano e se a correção é proporcional.

## Dependências permitidas

Esta skill deve ser autocontida. Use apenas:
- Helper Python: `.agents/skills/fix-pr/scripts/fix_pr_context.py`
- Roteamento e regras de agentes: `AGENTS.md`
- Se disponível no projeto, utilize a skill de `code-review` local (`.agents/skills/code-review/SKILL.md`) ou o próprio harness de testes do projeto para auto-revisão local antes de publicar.

*Restrição:* Não crie chamadas REST inline, curl, Invoke-RestMethod ou scripts temporários para GitHub/Azure DevOps. Todas as interações com a API remota de pull request (coleta e resolução) devem passar exclusivamente pelo helper Python `fix_pr_context.py`.

## Gatilho

Use quando o usuário pedir para corrigir, revisar, simular ou resolver comentários/threads de uma PR. Ex: *"Use fix-pr para corrigir os comentários da PR 12"* ou *"Use fix-pr em dry-run na PR 12"*.

## Diretório de execução

Todos os artefatos temporários devem ficar no diretório da rodada (não commitar):
- Pasta por rodada: `.agents/skills/fix-pr/runs/pr-XXX/`
- Arquivos: `context.json`, `plan-gate.md`, `plan-exec.md`, `thread-YYYY.state.md` (onde YYYY é o ID da thread, que no GitHub pode ser uma string)

Artefato **commitável** (gerado ao final):
- Report da rodada: `.cursor/codereviews/PR-XXX-rodada-N.md`

## Fluxo obrigatório

### 0. Coleta e Resolução no Remote (obrigatório)

Execute a partir da raiz do workspace:
```bash
# Coletar contexto
python .agents/skills/fix-pr/scripts/fix_pr_context.py collect --pr-id XXX --output .agents/skills/fix-pr/runs/pr-XXX/context.json

# Resolver thread (normal)
python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread --pr-id XXX --thread-id YYYY --comment "Justificativa..."

# Resolver thread (dry-run)
python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread --dry-run --pr-id XXX --thread-id YYYY --comment "Justificativa..."
```
Se a coleta falhar, pare e reporte. Não improvise acessos.

### 1. Montar contexto real

Leia `context.json` e reúna: comentário original, arquivo/linha, descrição das issues/Work Items vinculados, planos locais (`.cursor/plans/`), código atual do trecho e regras do cursor aplicáveis.

### 2. Julgar cada thread ativa

Avalie cada thread técnica sob os seguintes critérios:
1. O caminho é executável e provável de falhar?
2. Há coerência com a issue/Work Item e o plano?
3. O código atual já se protege contra isso (invariants, validations, testes)?
4. O impacto é material (segurança, perda de dados, regras financeiras/fiscais)?
5. A alteração é proporcional ou é overengineering/nit?

Calibre a urgência/criticidade (0–10) e classifique a ação:

| Score | Urgência | Significado / Ação Recomendada |
|---|---|---|
| **0–2** | Nível Baixo | Nit, estilo, preferência. -> **Resolver sem código** |
| **3–5** | Nível Baixo | Risco baixo ou warning improvável. -> **Resolver sem código** |
| **6–8** | Alta | Bug provável, regressão ou desalinhamento. -> **Corrigir em código** |
| **9–10** | Alta | Crítico (segurança, dados, regras vitais). -> **Corrigir em código** |

*Nota:* Se houver conflito entre issue/Work Item/plano/comentário que exija decisão de produto, classifique como **Escalar**.

### 3. Gate de confirmação do usuário (obrigatório)

Antes de qualquer edição ou `resolve-thread`, apresente o plano e **pare** para confirmação do usuário.
Grave o plano em `.agents/skills/fix-pr/runs/pr-XXX/plan-gate.md` (temporário, não commitar) dividido nas seções:
- **Corrigir em código** (Threads com Score > 5)
- **Resolver com comentário (sem alterar código)** (Threads com Score <= 5)
- **Escalar** (Aguardar decisão humana)

*Colunas das tabelas:* `Thread` | `Arquivo` | `Score` | `Urgência` | `Justificativa resumida`.

Apresente o resumo numérico e pergunte **exatamente**:
```text
Deseja efetuar as correções ou finalização das threads [ID1, ID2]?
```
- Se o usuário recusar/não confirmar, não prossiga. Se solicitado limpeza, apague recursivamente a pasta `runs/pr-XXX/` (exceto `.gitignore`).
- Se pedir alteração de classificação, atualize a tabela e refaça a pergunta.

### 3.1. Plano de execução (pós-gate)

Após confirmação do gate 3:
1. Crie `.agents/skills/fix-pr/runs/pr-XXX/plan-exec.md` detalhando as tarefas de execução. O cabeçalho deve conter:
   - Metadata básica (PR, Rodada, Gate Aprovado em, Modo, Contexto).
   - Detalhamento por thread aprovada: `ThreadId`, `Ação`, `Arquivo/linha`, `Estratégia`, `Arquivos permitidos`, `Subagent` (sim/não + prompt), `Testes` e `Checklist`.
   - Seção **Report commitável** prevendo o layout final (conforme Etapa 6).
   - Checklist operacional de publicação (Etapas 4–6).
2. Informe o caminho de `plan-exec.md` e pergunte **exatamente**:
```text
Deseja executar o plano normalmente?
```
3. Execute as etapas 4–6 apenas se confirmado. Se recusado, ofereça limpeza da pasta temporária.

### 4. Resolver sem código em nível baixo de urgência

Para threads aprovadas como `Resolver com comentário`, comente curto e objetivo:
```bash
python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread --pr-id XXX --thread-id YYYY --comment "Sem alteração de código: o cenário citado já é bloqueado por X; a issue não exige Y; risco real baixo por Z."
```
(Adicione `--dry-run` se aplicável).

### 5. Corrigir somente o que faz sentido — fix à prova de pipeline

Para cada thread aprovada para `Corrigir em código`:
1. Mapeie a estratégia contra a issue.
2. Analise os impactos locais: chamadores, DTOs, validações e fluxos adjacentes.
3. **Corrija a classe do defeito, não só a instância (obrigatório).** Antes de fechar a thread, faça `grep`/`glob` por **todas as ocorrências irmãs do mesmo padrão** no diff da PR. Corrija todas de uma vez e cubra a classe com teste anti-regressão.
4. Crie `.agents/skills/fix-pr/runs/pr-XXX/thread-YYYY.state.md` contendo: problema, estratégia, **ocorrências irmãs encontradas (classe)**, caminhos analisados, riscos residuais e plano de teste.
5. Execute via subagent para isolar e validar a alteração.
   - *Instruções do subagent:* Deve resolver a thread **e toda a classe do defeito**, criar/ajustar teste anti-regressão, verificar se resiste à pipeline e documentar a alteração. Fix mínimo e de alta qualidade.
6. Evite violações de arquitetura, validações ausentes de DateTime/Enums e falhas de segurança.

### 6. Validar, reportar, fechar threads e publicar

1. Rode build/testes relevantes no projeto (tanto no backend/C# quanto no frontend/Angular).
2. Se a skill de `code-review` estiver presente, execute-a localmente para realizar a auto-revisão.
3. Se houve correção de código, gere o relatório commitável em `.cursor/codereviews/PR-XXX-rodada-N.md` com a estrutura:
   - **Resumo**: PR, rodada, threads tratadas, testes rodados, arquivos alterados.
   - **Por thread corrigida**: problema, o que foi feito, como foi corrigido, caminhos analisados, testes anti-regressão (ou justificativa) e riscos residuais.
   - **Por thread sem código**: justificativa e por que não houve mudança.
4. Resolva cada thread via `resolve-thread` com comentários explicativos (referenciando o relatório para as threads corrigidas em código).
5. Stage e Commit cirúrgicos (nunca `git add .`):
   - Inclua os fixes e o relatório `.cursor/codereviews/PR-XXX-rodada-N.md`.
   - Mensagem de commit obrigatória: `Fix PR XXX: thread(s): [thread1, thread2] (rodada N)`.
   - Faça `git push` (a menos que em `dry-run`).

## Resposta final obrigatória

Seu report final deve conter:
1. Resumo das threads tratadas e a decisão (Corrigida, Resolvida ou Escalada).
2. Justificativa curta por decisão, impacto real e testes executados.
3. Link/caminho do report `.cursor/codereviews/PR-XXX-rodada-N.md` criado.
4. Arquivos alterados/commitados (ou simulação local se em `dry-run`).
5. Hash/mensagem do commit e confirmação de push (ou aviso de dry-run).

## Referências
- `AGENTS.md`
- Helper Python: `.agents/skills/fix-pr/scripts/fix_pr_context.py`
