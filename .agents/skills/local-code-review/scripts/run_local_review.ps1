# Script para executar o agentic-code-reviewers localmente no Windows em modo dry-run.

$ErrorActionPreference = 'Stop'

# 1. Carregar OPENCODE_API_KEY
if (-not $env:OPENCODE_API_KEY) {
    # Procura .env na raiz do projeto atual
    $envPath = Join-Path $PSScriptRoot "..\..\..\.env"
    if (Test-Path $envPath) {
        $keyLine = Select-String -Path $envPath -Pattern '^\s*OPENCODE_API_KEY\s*=\s*([^\s#]+)'
        if ($keyLine) {
            $env:OPENCODE_API_KEY = $keyLine.Matches.Groups[1].Value.Trim()
        }
    }
}

if (-not $env:OPENCODE_API_KEY) {
    # Procura no local conhecido L:\source\agentic-code-reviewers\.env
    $otherEnvPath = "L:\source\agentic-code-reviewers\.env"
    if (Test-Path $otherEnvPath) {
        $keyLine = Select-String -Path $otherEnvPath -Pattern '^\s*OPENCODE_API_KEY\s*=\s*([^\s#]+)'
        if ($keyLine) {
            $env:OPENCODE_API_KEY = $keyLine.Matches.Groups[1].Value.Trim()
        }
    }
}

if (-not $env:OPENCODE_API_KEY) {
    Write-Host "A variavel de ambiente OPENCODE_API_KEY nao foi encontrada. Se estiver usando o engine opencode, configure-a no .env."
}

# 2. Obter a branch atual (Source) e Target
$currentBranch = git branch --show-current
if (-not $currentBranch) {
    $currentBranch = (git rev-parse --abbrev-ref HEAD).Trim()
}

$sourceBranchRef = "refs/heads/$currentBranch"
$targetBranchRef = if ($env:AGENTIC_CODE_REVIEWERS_TARGET_BRANCH) { $env:AGENTIC_CODE_REVIEWERS_TARGET_BRANCH } else { "refs/heads/main" }

# 3. Baixar run.sh se nao existir ou sempre atualizar
$runsDir = Join-Path $PSScriptRoot "..\runs"
if (-not (Test-Path $runsDir)) {
    New-Item -ItemType Directory -Force -Path $runsDir | Out-Null
}

$scriptPath = Join-Path $runsDir "run.sh"
$url = "https://raw.githubusercontent.com/jpolvora/agentic-code-reviewers/main/run.sh"

Write-Host "Baixando runner do agentic-code-reviewers..."
Invoke-WebRequest -Uri $url -OutFile $scriptPath -UseBasicParsing

# 4. Localizar Git Bash (bash.exe)
$bashExe = "C:\Program Files\Git\bin\bash.exe"
if (-not (Test-Path $bashExe)) {
    $bashExe = (Get-Command bash.exe -ErrorAction SilentlyContinue).Source
}

if (-not $bashExe -or -not (Test-Path $bashExe)) {
    Write-Error "Nao foi possivel localizar o Git Bash (bash.exe). Certifique-se de que o Git esta instalado no Windows."
}

# Converter caminhos para POSIX
$posixScriptPath = $scriptPath -replace '\\', '/'

# Executar via Git Bash
Write-Host "Executando agentic-code-reviewers localmente (dry-run) para $sourceBranchRef -> $targetBranchRef..."

$extraArgs = ""
if ($args) {
    $extraArgs = $args -join " "
}

& $bashExe -c "bash '$posixScriptPath' --dry-run --source-branch '$sourceBranchRef' --target-branch '$targetBranchRef' $extraArgs"
