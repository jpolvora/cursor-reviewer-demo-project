# Agentic Code Reviewers Demo Project

This is a demonstration project showcasing how to integrate **[agentic-code-reviewers](https://github.com/jpolvora/agentic-code-reviewers)** — a multi-agent, AI-powered pull request reviewer — running in a **GitHub Actions** CI/CD pipeline.

The repository simulates a fullstack application with a **C# ASP.NET Core 10.0** backend and an **Angular 19.2** frontend, including authentication, document management, and an audit log system.

See [FEATURES.md](./FEATURES.md) for the complete list of features.

---

## 🚀 How it Works

The GitHub Actions workflow [.github/workflows/code-review.yml](.github/workflows/code-review.yml) triggers on pull requests targeting the `main` branch.

When triggered, it:
1. Performs a full history checkout (`fetch-depth: 0`).
2. Creates and checks out a local branch tracking the PR head branch to support fork-based PRs without authentication/fetch failures.
3. Fetches and runs `run.sh` from the **`release`** branch of `agentic-code-reviewers` (compiled runtime artifacts).
4. Performs an agentic AI review using `cursor-sdk` / `composer-2.5` over the files changed in the PR.
5. Posts actionable review threads directly onto the GitHub Pull Request.
6. Fails the job if active bot threads remain — which triggers the **Auto Fix** pipeline ([auto-fix.yml](.github/workflows/auto-fix.yml)) to resolve them.

---

## 🛠️ GitHub Actions Setup

To enable this review pipeline in your repository:

### 1. Configure Secrets

Ensure the following secrets are configured in your GitHub repository (**Settings > Secrets and variables > Actions**):

| Secret | Description |
|--------|-------------|
| `CURSOR_API_KEY` | Cursor API key for the review engine (`cursor-sdk`). |
| `OPENCODE_API_KEY` | OpenCode API key for the auto-fix engine (`opencode`). |
| `AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN` | PAT with `repo` / pull-requests write — required for push, thread resolution, and workflow chaining. Falls back to `github.token` for publishing only. |

### 2. Workflow Permissions

The workflow requires permissions to write comments on the pull request. This is configured in the job definitions:

```yaml
permissions:
  contents: read
  pull-requests: write
```

---

## 📁 Repository Structure

*   **`backend/`** — ASP.NET Core 10.0 Web API (C#) with SQLite, authentication, document management, and audit logging.
*   **`frontend/`** — Angular 19.2 SPA (TypeScript) with login, dashboard, profile, document portal, and audit log views.
*   **`.github/workflows/code-review.yml`** — PR-triggered AI code review pipeline.
*   **`.github/workflows/auto-fix.yml`** — Post-review auto-fix pipeline that resolves review threads automatically.
*   **`.agents/skills/`** — Agentic workflow definitions for IDE-level code review, PR fixing, and multi-round thread tracking.
*   **`.cursor/`** — Cursor IDE configuration and MCP server settings.
*   **`FEATURES.md`** — Complete features breakdown.
*   **`AGENTS.md`** — Developer agent guidelines for AI-assisted development.
