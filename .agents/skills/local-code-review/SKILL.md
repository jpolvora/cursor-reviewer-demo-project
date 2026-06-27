---
name: local-code-review
description: Executa o cursor-reviewer localmente no ambiente Windows para analisar o diff e relatar erros/propor melhorias de forma simulada (dry-run).
---

# local-code-review

## Objetivo

Executar o agente `cursor-reviewer` localmente no ambiente Windows para revisar alterações (uncommitted ou diff entre branches) no repositório atual, simulando as regras da pipeline de CI/CD em modo leitura (dry-run).

## Dependências

- Git Bash (`bash.exe`) instalado no Windows.
- Chave de API do Cursor (`CURSOR_API_KEY`). O script a carregará automaticamente do ambiente ou de arquivos `.env` locais/conhecidos.
- Script auxiliar: `.agents/skills/local-code-review/scripts/run_local_review.ps1`

## Gatilho

Use quando o usuário pedir para rodar uma análise local, simular a pipeline, revisar o código atual localmente, ou validar se existem erros antes de realizar um push. Ex:
- *"Rode local-code-review"*
- *"Rode a revisão local com alterações não commitadas"*
- *"Simule a pipeline localmente"*

## Parâmetros Suportados

Você pode passar parâmetros adicionais ao script PowerShell:
- `--include-uncommitted`: Inclui modificações na working tree (staged/unstaged) no escopo da revisão.
- `--verbose`: Ativa logs detalhados do agente.

## Como Executar

Execute o script PowerShell a partir da raiz do repositório:
```powershell
powershell -File .agents/skills/local-code-review/scripts/run_local_review.ps1
```

Exemplo incluindo arquivos não commitados:
```powershell
powershell -File .agents/skills/local-code-review/scripts/run_local_review.ps1 --include-uncommitted
```

## Resposta Final do Agente

Ao finalizar a execução, resuma o feedback do `cursor-reviewer`:
1. Quais arquivos foram analisados.
2. Quais problemas (erros/melhorias) foram reportados (separados por severidade: Critical, Warning, Suggestion).
3. Quais são as correções propostas para cada problema.
