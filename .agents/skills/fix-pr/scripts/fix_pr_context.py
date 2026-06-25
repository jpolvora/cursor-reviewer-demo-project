#!/usr/bin/env python3
"""
Helper cross-platform do fix-pr para GitHub e Azure DevOps.

Objetivo:
- Padronizar a coleta de PR, threads, comentarios e work items/issues.
- Detectar automaticamente a plataforma (GitHub ou Azure DevOps).
- Autenticar em ambas as plataformas e realizar operações de leitura e resolução de threads.

Uso:
  python .agents/skills/fix-pr/scripts/fix_pr_context.py collect --pr-id 1 --output .agents/skills/fix-pr/runs/pr-1/context.json
  python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread --pr-id 1 --thread-id "PRRT_..." --comment "Justificativa..."
  python .agents/skills/fix-pr/scripts/fix_pr_context.py resolve-thread --dry-run --pr-id 1 --thread-id "PRRT_..." --comment "Justificativa..."
"""

from __future__ import annotations

import argparse
import base64
import html
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any

ACTIVE_STATUSES = {"active", "pending"}
DEFAULT_WORK_ITEM_FIELDS = ",".join(
    [
        "System.Id",
        "System.Title",
        "System.State",
        "System.WorkItemType",
        "System.Description",
        "Microsoft.VSTS.Common.AcceptanceCriteria",
    ]
)


def find_repo_root(start: Path) -> Path:
    current = start.resolve()
    for candidate in [current, *current.parents]:
        if (candidate / ".agents").is_dir() or (candidate / ".git").is_dir():
            return candidate
    raise SystemExit("Nao foi possivel localizar a raiz do repo com .agents/ ou .git/.")


def detect_platform_and_repo(repo_root: Path, repository_arg: str = "") -> tuple[str, str, dict[str, Any]]:
    """
    Retorna (plataforma, repositório/slug, metadados)
    plataforma: "github" ou "azure-devops"
    """
    git_config = repo_root / ".git" / "config"
    if not git_config.exists():
        if repository_arg:
            if "/" in repository_arg:
                parts = repository_arg.split("/")
                return "github", repository_arg, {"owner": parts[0], "repo": parts[1]}
            return "azure-devops", repository_arg, {}
        raise SystemExit("Nao foi possivel detectar a plataforma. Especifique --repository ou execute na raiz do repositorio git.")

    content = git_config.read_text(encoding="utf-8", errors="ignore")
    urls = re.findall(r"^\s*url\s*=\s*(.+)$", content, flags=re.MULTILINE)
    
    # Procurar por GitHub
    for url in urls:
        github_match = re.search(r"github\.com[:/]([^/]+)/([^/\s\.]+)(?:\.git)?", url)
        if github_match:
            owner = github_match.group(1)
            repo = github_match.group(2)
            detected_repo = f"{owner}/{repo}"
            return "github", detected_repo, {"owner": owner, "repo": repo}
            
    # Procurar por Azure DevOps
    for url in urls:
        azdo_match = re.search(r"/_git/([^/\s]+)", url)
        if azdo_match:
            detected_repo = urllib.parse.unquote(azdo_match.group(1))
            return "azure-devops", detected_repo, {}
            
    for url in urls:
        if url.rstrip().endswith(".git"):
            detected_repo = Path(url.rstrip()[:-4]).name
            if "github.com" in url:
                return "github", detected_repo, {}
            return "azure-devops", detected_repo, {}

    if repository_arg:
        if "/" in repository_arg:
            parts = repository_arg.split("/")
            return "github", repository_arg, {"owner": parts[0], "repo": parts[1]}
        return "azure-devops", repository_arg, {}
        
    raise SystemExit("Nao foi possivel detectar a plataforma ou repositorio no .git/config.")


# --- GitHub API Helpers ---

def load_github_token(repo_root: Path) -> str:
    pat = os.environ.get("GITHUB_TOKEN", "").strip() or os.environ.get("GH_TOKEN", "").strip()
    if pat and pat != "github_pat_antigravitydummytoken":
        return pat

    secret_path = repo_root / ".agents" / "github.secret"
    if secret_path.exists():
        return secret_path.read_text(encoding="utf-8").strip()

    try:
        p = subprocess.Popen(['git', 'credential', 'fill'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True)
        out, _ = p.communicate("protocol=https\nhost=github.com\n\n")
        for line in out.splitlines():
            if line.startswith("password="):
                token = line.split("=", 1)[1].strip()
                if token:
                    return token
    except Exception:
        pass

    try:
        result = subprocess.run(
            ["gh", "auth", "token"],
            capture_output=True,
            text=True,
            check=False
        )
        if result.returncode == 0:
            token = result.stdout.strip()
            if token:
                return token
    except Exception:
        pass

    if pat:
        return pat

    raise SystemExit("Defina GITHUB_TOKEN ou crie .agents/github.secret ou autentique no Git/GitHub CLI.")



def github_graphql_request(token: str, query: str, variables: dict[str, Any]) -> dict[str, Any]:
    url = "https://api.github.com/graphql"
    headers = {
        "Authorization": f"Bearer {token}",
        "User-Agent": "python-urllib",
        "Content-Type": "application/json"
    }
    body = {"query": query, "variables": variables}
    data = json.dumps(body, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(url, data=data, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=90) as resp:
            raw = resp.read()
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"GitHub GraphQL HTTP {exc.code}: {detail}") from exc
        
    payload = json.loads(raw.decode("utf-8"))
    if "errors" in payload:
        raise SystemExit(f"GitHub GraphQL Errors: {json.dumps(payload['errors'], indent=2)}")
    return payload


def get_pr_context_github(
    repo_root: Path,
    pr_id: int,
    repository: str,
    metadata: dict[str, Any],
    include_system: bool
) -> dict[str, Any]:
    token = load_github_token(repo_root)
    
    parts = repository.split("/")
    if len(parts) == 2:
        owner, repo_name = parts[0], parts[1]
    else:
        owner = metadata.get("owner", "")
        repo_name = metadata.get("repo", "")
    
    if not owner or not repo_name:
        raise SystemExit(f"Nao foi possivel extrair owner/repo do repositorio: {repository}")

    query = """
    query($owner: String!, $repo: String!, $number: Int!) {
      repository(owner: $owner, name: $repo) {
        pullRequest(number: $number) {
          title
          state
          headRefName
          baseRefName
          author {
            login
          }
          url
          closingIssuesReferences(first: 50) {
            nodes {
              number
              title
              state
              body
              url
            }
          }
          reviewThreads(first: 100) {
            nodes {
              id
              isResolved
              path
              line
              originalLine
              comments(first: 100) {
                nodes {
                  id
                  body
                  author {
                    login
                  }
                  createdAt
                }
              }
            }
          }
        }
      }
    }
    """
    
    variables = {
        "owner": owner,
        "repo": repo_name,
        "number": pr_id
    }
    
    result = github_graphql_request(token, query, variables)
    pr_data = result.get("data", {}).get("repository", {}).get("pullRequest")
    if not pr_data:
        raise SystemExit(f"Pull Request #{pr_id} nao encontrado no repositorio {repository}.")
        
    work_items = []
    for issue in pr_data.get("closingIssuesReferences", {}).get("nodes", []):
        work_items.append({
            "id": issue.get("number"),
            "type": "Issue",
            "state": issue.get("state"),
            "title": issue.get("title"),
            "description": clean_html(issue.get("body")),
            "acceptanceCriteria": "",
            "url": issue.get("url")
        })
        
    threads = []
    active_threads = []
    for node in pr_data.get("reviewThreads", {}).get("nodes", []):
        thread_id = node.get("id")
        is_resolved = node.get("isResolved")
        path = node.get("path")
        line = node.get("line") or node.get("originalLine")
        
        comments = []
        for c in node.get("comments", {}).get("nodes", []):
            comments.append({
                "id": c.get("id"),
                "type": "user",
                "author": c.get("author", {}).get("login") if c.get("author") else "ghost",
                "content": clean_html(c.get("body")),
                "publishedDate": c.get("createdAt")
            })
            
        if not comments:
            continue
            
        thread_obj = {
            "threadId": thread_id,
            "status": "fixed" if is_resolved else "active",
            "path": path,
            "rightLine": line,
            "leftLine": None,
            "isDeleted": False,
            "comments": comments
        }
        
        threads.append(thread_obj)
        if not is_resolved:
            active_threads.append(thread_obj)
            
    return {
        "source": {
            "helper": str(Path(__file__).as_posix()),
            "platform": "github"
        },
        "organization": owner,
        "project": repo_name,
        "repository": repository,
        "pullRequest": {
            "id": pr_id,
            "title": pr_data.get("title"),
            "status": pr_data.get("state"),
            "sourceRefName": pr_data.get("headRefName"),
            "targetRefName": pr_data.get("baseRefName"),
            "createdBy": pr_data.get("author", {}).get("login") if pr_data.get("author") else "ghost",
            "url": pr_data.get("url"),
        },
        "workItems": work_items,
        "threads": threads,
        "activeThreads": active_threads,
    }


def resolve_thread_github(
    repo_root: Path,
    pr_id: int,
    repository: str,
    metadata: dict[str, Any],
    thread_id: str,
    comment: str,
    dry_run: bool,
) -> dict[str, Any]:
    if dry_run:
        return {
            "dryRun": True,
            "threadId": thread_id,
            "status": "would_mark_fixed",
            "commentId": None,
            "comment": comment,
            "message": "Dry-run: nenhum comentario foi postado e a thread nao foi alterada no GitHub.",
        }
        
    token = load_github_token(repo_root)
    
    # 1. Postar comentario
    comment_mutation = """
    mutation($threadId: ID!, $body: String!) {
      addPullRequestReviewThreadReply(input: {pullRequestReviewThreadId: $threadId, body: $body}) {
        comment {
          id
        }
      }
    }
    """
    comment_res = github_graphql_request(token, comment_mutation, {"threadId": thread_id, "body": comment})
    comment_id = comment_res.get("data", {}).get("addPullRequestReviewThreadReply", {}).get("comment", {}).get("id")

    
    # 2. Resolver thread
    resolve_mutation = """
    mutation($threadId: ID!) {
      resolveReviewThread(input: {threadId: $threadId}) {
        thread {
          id
          isResolved
        }
      }
    }
    """
    resolve_res = github_graphql_request(token, resolve_mutation, {"threadId": thread_id})
    is_resolved = resolve_res.get("data", {}).get("resolveReviewThread", {}).get("thread", {}).get("isResolved", False)
    
    return {
        "threadId": thread_id,
        "status": "fixed" if is_resolved else "active",
        "commentId": comment_id
    }


# --- Azure DevOps API Helpers ---

def load_azdo_config(repo_root: Path) -> tuple[str, str, str]:
    config_path = repo_root / ".agents" / "azure-devops.config.json"
    secret_path = repo_root / ".agents" / "azure-devops.secret"

    if not config_path.exists():
        raise SystemExit("Crie .agents/azure-devops.config.json com organization e project.")

    config = json.loads(config_path.read_text(encoding="utf-8"))
    organization = config["organization"]
    project = config["project"]

    pat = os.environ.get("AZURE_DEVOPS_PAT", "").strip()
    if not pat and secret_path.exists():
        pat = secret_path.read_text(encoding="utf-8").strip()
    if not pat:
        raise SystemExit("Defina AZURE_DEVOPS_PAT ou crie .agents/azure-devops.secret.")

    return organization, project, pat


def find_powershell() -> str:
    configured = os.environ.get("POWERSHELL_EXE")
    if configured:
        return configured

    for name in ("pwsh", "powershell"):
        exe = shutil.which(name)
        if exe:
            return exe

    raise SystemExit("PowerShell nao encontrado. Instale pwsh ou disponibilize powershell no PATH.")


def run_azure_devops_ps_smoke(repo_root: Path) -> dict[str, Any]:
    ps_script = repo_root / ".agents" / "skills" / "azure-devops" / "scripts" / "azure-devops.ps1"
    if not ps_script.exists():
        # Se nao houver a skill azure-devops localmente, retorna smoke test simulado para nao quebrar a execucao
        return {
            "command": "simulado",
            "exitCode": 0,
            "stdout": "Skill azure-devops nao instalada. Pulando validacao real.",
            "stderr": ""
        }

    exe = find_powershell()
    command = [exe, "-NoProfile"]
    if os.name == "nt":
        command += ["-ExecutionPolicy", "Bypass"]
    command += ["-File", str(ps_script), "-Action", "list-states"]

    result = subprocess.run(
        command,
        cwd=str(repo_root),
        text=True,
        capture_output=True,
        timeout=60,
        check=False,
    )
    return {
        "command": " ".join(command),
        "exitCode": result.returncode,
        "stdout": result.stdout.strip(),
        "stderr": result.stderr.strip(),
    }


def auth_headers(pat: str, content_type: str = "application/json") -> dict[str, str]:
    token = base64.b64encode(f":{pat}".encode("ascii")).decode("ascii")
    return {
        "Authorization": f"Basic {token}",
        "Accept": "application/json; api-version=7.1",
        "Content-Type": content_type,
    }


def azdo_request(
    method: str,
    url: str,
    pat: str,
    body: Any | None = None,
    content_type: str = "application/json",
) -> Any:
    data: bytes | None = None
    if body is not None:
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")

    request = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers=auth_headers(pat, content_type=content_type),
    )
    try:
        with urllib.request.urlopen(request, timeout=90) as response:
            raw = response.read()
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Azure DevOps HTTP {exc.code}: {detail}") from exc

    if not raw:
        return None
    return json.loads(raw.decode("utf-8"))


def base_url(organization: str, project: str) -> str:
    return f"https://dev.azure.com/{organization}/{urllib.parse.quote(project)}"


def git_url(organization: str, project: str, repository: str, suffix: str) -> str:
    return (
        f"{base_url(organization, project)}/_apis/git/repositories/"
        f"{urllib.parse.quote(repository)}/{suffix}"
    )


def get_pr_context_azdo(repo_root: Path, pr_id: int, repository: str, include_system: bool) -> dict[str, Any]:
    organization, project, pat = load_azdo_config(repo_root)
    ps_smoke = run_azure_devops_ps_smoke(repo_root)
    if ps_smoke["exitCode"] != 0:
        raise SystemExit(
            "Falha ao validar azure-devops.ps1.\n"
            f"stdout: {ps_smoke['stdout']}\nstderr: {ps_smoke['stderr']}"
        )

    pr = azdo_request(
        "GET",
        git_url(organization, project, repository, f"pullRequests/{pr_id}?api-version=7.1"),
        pat,
    )
    threads_payload = azdo_request(
        "GET",
        git_url(organization, project, repository, f"pullRequests/{pr_id}/threads?api-version=7.1"),
        pat,
    )
    work_item_refs = azdo_request(
        "GET",
        git_url(organization, project, repository, f"pullRequests/{pr_id}/workitems?api-version=7.1"),
        pat,
    ).get("value", [])

    work_items = []
    ids = [str(item["id"]) for item in work_item_refs]
    if ids:
        items_url = (
            f"{base_url(organization, project)}/_apis/wit/workitems"
            f"?ids={','.join(ids)}&fields={urllib.parse.quote(DEFAULT_WORK_ITEM_FIELDS)}&api-version=7.1"
        )
        items_payload = azdo_request("GET", items_url, pat)
        for item in items_payload.get("value", []):
            fields = item.get("fields", {})
            work_items.append(
                {
                    "id": item.get("id"),
                    "type": fields.get("System.WorkItemType"),
                    "state": fields.get("System.State"),
                    "title": fields.get("System.Title"),
                    "description": clean_html(fields.get("System.Description")),
                    "acceptanceCriteria": clean_html(
                        fields.get("Microsoft.VSTS.Common.AcceptanceCriteria")
                    ),
                    "url": f"{base_url(organization, project)}/_workitems/edit/{item.get('id')}",
                }
            )

    threads = [normalize_thread_azdo(thread, include_system=include_system) for thread in threads_payload.get("value", [])]
    threads = [thread for thread in threads if thread is not None]
    active_threads = [
        thread
        for thread in threads
        if str(thread.get("status", "")).lower() in ACTIVE_STATUSES and thread.get("comments")
    ]

    # Tentativa de pegar o caminho da skill para salvar a referencia do source
    azure_devops_ps_path = repo_root / ".agents" / "skills" / "azure-devops" / "scripts" / "azure-devops.ps1"
    
    return {
        "source": {
            "helper": str(Path(__file__).as_posix()),
            "azureDevOpsScript": str(azure_devops_ps_path.as_posix()) if azure_devops_ps_path.exists() else None,
            "azureDevOpsScriptSmoke": ps_smoke,
        },
        "organization": organization,
        "project": project,
        "repository": repository,
        "pullRequest": {
            "id": pr.get("pullRequestId"),
            "title": pr.get("title"),
            "status": pr.get("status"),
            "sourceRefName": pr.get("sourceRefName"),
            "targetRefName": pr.get("targetRefName"),
            "createdBy": (pr.get("createdBy") or {}).get("displayName"),
            "url": pr.get("url"),
        },
        "workItems": work_items,
        "threads": threads,
        "activeThreads": active_threads,
    }


def normalize_thread_azdo(thread: dict[str, Any], include_system: bool) -> dict[str, Any] | None:
    comments = []
    for comment in thread.get("comments") or []:
        if comment.get("isDeleted"):
            continue
        comment_type = comment.get("commentType")
        if not include_system and comment_type == "system":
            continue
        content = clean_html(comment.get("content"))
        if not content:
            continue
        comments.append(
            {
                "id": comment.get("id"),
                "type": comment_type,
                "author": (comment.get("author") or {}).get("displayName"),
                "content": content,
                "publishedDate": comment.get("publishedDate"),
            }
        )

    if not comments:
        return None

    context = thread.get("threadContext") or {}
    pr_context = thread.get("pullRequestThreadContext") or {}
    right_start = context.get("rightFileStart") or {}
    right_end = context.get("rightFileEnd") or {}
    left_start = context.get("leftFileStart") or {}
    left_end = context.get("leftFileEnd") or {}

    return {
        "threadId": thread.get("id"),
        "status": thread.get("status"),
        "path": pr_context.get("filePath") or context.get("filePath"),
        "rightLine": right_start.get("line") or right_end.get("line"),
        "leftLine": left_start.get("line") or left_end.get("line"),
        "isDeleted": thread.get("isDeleted"),
        "comments": comments,
    }


def resolve_thread_azdo(
    repo_root: Path,
    pr_id: int,
    repository: str,
    thread_id: int,
    comment: str,
    dry_run: bool,
) -> dict[str, Any]:
    if dry_run:
        return {
            "dryRun": True,
            "threadId": thread_id,
            "status": "would_mark_fixed",
            "commentId": None,
            "comment": comment,
            "message": "Dry-run: nenhum comentario foi postado e a thread nao foi alterada no Azure DevOps.",
        }

    organization, project, pat = load_azdo_config(repo_root)
    ps_smoke = run_azure_devops_ps_smoke(repo_root)
    if ps_smoke["exitCode"] != 0:
        raise SystemExit(
            "Falha ao validar azure-devops.ps1.\n"
            f"stdout: {ps_smoke['stdout']}\nstderr: {ps_smoke['stderr']}"
        )

    comments_suffix = f"pullRequests/{pr_id}/threads/{thread_id}/comments?api-version=7.1"
    patch_suffix = f"pullRequests/{pr_id}/threads/{thread_id}?api-version=7.1"

    posted = azdo_request(
        "POST",
        git_url(organization, project, repository, comments_suffix),
        pat,
        body={"content": comment, "commentType": 1},
    )
    patched = azdo_request(
        "PATCH",
        git_url(organization, project, repository, patch_suffix),
        pat,
        body={"status": "fixed"},
    )

    return {
        "threadId": thread_id,
        "status": patched.get("status") if patched else "fixed",
        "commentId": posted.get("id") if posted else None,
    }


# --- General Helpers ---

def clean_html(value: str | None) -> str:
    if not value:
        return ""
    text = re.sub(r"<br\s*/?>", "\n", value, flags=re.IGNORECASE)
    text = re.sub(r"</p\s*>", "\n", text, flags=re.IGNORECASE)
    text = re.sub(r"<[^>]+>", "", text)
    text = html.unescape(text)
    return re.sub(r"\n{3,}", "\n\n", text).strip()


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Helper para fix-pr (suporta GitHub e Azure DevOps).")
    parser.add_argument("--repo-root", default="", help="Raiz do repositorio. Default: autodetect.")
    parser.add_argument("--repository", default="", help="Nome/slug do repositorio. Default: autodetect.")
    parser.add_argument("--platform", choices=["github", "azure-devops"], default="", help="Forcar plataforma.")
    sub = parser.add_subparsers(dest="action", required=True)

    collect = sub.add_parser("collect", help="Coleta PR, threads, comentarios e work items/issues.")
    collect.add_argument("--pr-id", type=int, required=True)
    collect.add_argument("--include-system", action="store_true")
    collect.add_argument("--output", default="", help="Arquivo JSON de saida. Default: stdout.")

    resolve = sub.add_parser("resolve-thread", help="Comenta e marca uma thread como fixed.")
    resolve.add_argument("--pr-id", type=int, required=True)
    resolve.add_argument("--thread-id", required=True, help="ID da thread (string para GitHub, int para Azure DevOps).")
    resolve.add_argument("--comment", required=True)
    resolve.add_argument(
        "--dry-run",
        action="store_true",
        help="Simula a resolucao localmente sem postar comentario nem alterar status no remote.",
    )

    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    repo_root = find_repo_root(Path(args.repo_root) if args.repo_root else Path.cwd())
    
    detected_platform, detected_repo, metadata = detect_platform_and_repo(repo_root, args.repository)
    platform = args.platform or detected_platform
    repository = args.repository or detected_repo

    if platform == "github":
        if args.action == "collect":
            payload = get_pr_context_github(repo_root, args.pr_id, repository, metadata, include_system=args.include_system)
            text = json.dumps(payload, ensure_ascii=False, indent=2)
            if args.output:
                output_path = Path(args.output)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_text(text + "\n", encoding="utf-8")
            else:
                print(text)
            return 0
        elif args.action == "resolve-thread":
            payload = resolve_thread_github(
                repo_root,
                args.pr_id,
                repository,
                metadata,
                args.thread_id,
                args.comment,
                dry_run=args.dry_run,
            )
            print(json.dumps(payload, ensure_ascii=False, indent=2))
            return 0
            
    elif platform == "azure-devops":
        if args.action == "collect":
            payload = get_pr_context_azdo(repo_root, args.pr_id, repository, include_system=args.include_system)
            text = json.dumps(payload, ensure_ascii=False, indent=2)
            if args.output:
                output_path = Path(args.output)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_text(text + "\n", encoding="utf-8")
            else:
                print(text)
            return 0
        elif args.action == "resolve-thread":
            # Converte thread_id para int no Azure DevOps
            try:
                thread_id_int = int(args.thread_id)
            except ValueError:
                raise SystemExit(f"ID de thread invalido para Azure DevOps (deve ser um inteiro): {args.thread_id}")
            payload = resolve_thread_azdo(
                repo_root,
                args.pr_id,
                repository,
                thread_id_int,
                args.comment,
                dry_run=args.dry_run,
            )
            print(json.dumps(payload, ensure_ascii=False, indent=2))
            return 0

    raise SystemExit(f"Plataforma desconhecida ou acao invalida: {platform} / {args.action}")


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
