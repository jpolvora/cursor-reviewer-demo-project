# local-code-review

Skill para executar o `cursor-reviewer` localmente de forma simples no Windows, simulando a pipeline de CI/CD em modo somente leitura (dry-run).

## Objetivo

Esta skill automatiza:
1. A coleta e resolução da chave `CURSOR_API_KEY`.
2. A detecção automática da branch Git atual e da branch target (padrão: `refs/heads/main` ou `refs/heads/master`).
3. O download dinâmico e atualização do runner oficial do `cursor-reviewer`.
4. A execução local do agente com saída no terminal, permitindo encontrar erros e propor melhorias antes de enviar a branch para a PR.

## Como usar

Execute na raiz do repositório:
```powershell
powershell -File .agents/skills/local-code-review/scripts/run_local_review.ps1
```

### Incluindo alterações não commitadas (staged/unstaged/untracked)

Para validar modificações locais que ainda não foram salvas em commits:
```powershell
powershell -File .agents/skills/local-code-review/scripts/run_local_review.ps1 --include-uncommitted
```

## Como funciona

O script `.agents/skills/local-code-review/scripts/run_local_review.ps1` realiza as seguintes etapas:
- Busca `CURSOR_API_KEY` no ambiente ou lê de arquivos `.env` próximos.
- Baixa o script oficial compilado `run.sh` no diretório temporário `runs/`.
- Localiza o Git Bash (`bash.exe`) instalado no Windows.
- Executa a revisão via Bash no Windows no modo `dry-run`, direcionando a saída formatada de review diretamente ao console.
