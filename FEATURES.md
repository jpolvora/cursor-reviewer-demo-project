# Features

## Backend — ASP.NET Core 10.0 Web API

### Authentication & Session Management
- **Login** (`POST /api/auth/login`) — Username/password authentication with salted SHA-256 hashing, returns Bearer token (7-day expiry)
- **Logout** (`POST /api/auth/logout`) — Invalidates session token in SQLite
- **Current User** (`GET /api/auth/me`) — Returns authenticated user's username
- **System Stats** (`GET /api/auth/stats`) — Returns total users, active sessions, server time
- **User Charm** (`GET /api/auth/charm`, `POST /api/auth/charm/reroll`) — Extends `User` with lucky number (1–999), emoji, and tagline; reroll writes audit + session activity entries
- **Update Profile** (`PUT /api/auth/profile`) — Change username
- **Change Password** (`POST /api/auth/change-password`) — Validates current password, hashes new password with fresh salt, invalidates all existing sessions

### Document Management
- **List Documents** (`GET /api/documents/list`) — Lists files in `Uploads/` with name, size, creation time
- **Download Document** (`GET /api/documents/download?fileName=...`) — Downloads files with path traversal protection
- **Checksum** (`POST /api/documents/checksum`) — SHA-256 checksum for arbitrary string content

### Audit Log
- **All Logs** (`GET /api/audit`) — Paginated list of all system audit entries (action, user, details, IP, timestamp)
- **My Logs** (`GET /api/audit/my`) — Current user's personal audit trail
- **Logs by User** (`GET /api/audit/user/{userId}`) — Filter audit entries by specific user
- **Automatic Logging** — Auth events (login, logout, profile update, password change) and document events (download, list) are automatically recorded with client IP and timestamp
- **Entity**: `AuditLog` — FK to `User`, `Action`, `Details`, `IpAddress`, `Timestamp`

### Infrastructure
- **SQLite** via Entity Framework Core — auto-created database (`auth_demo.db`)
- **CORS** configured for Angular dev server (`localhost:4200`)
- **Auto-seeding** — default admin user (`admin` / `admin123`) on first run

---

## Frontend — Angular 19.2 SPA

### Pages & Components
- **Login** (`/login`) — Username/password form with validation, loading spinner, error handling, auto-redirect if already authenticated
- **Dashboard** (`/dashboard`) — Welcome message, **user charm panel** (lucky number reroll), system stats display (total users, active sessions, server time), logout
- **Profile** (`/profile`) — Update username form, pre-populated current username, success/error banners
- **Documents** (`/documents`) — Document listing with file metadata, click-to-select download, blob-based file download, WCAG-accessible inputs
- **Audit Log** (`/audit`) — Full audit trail table with action badges, filter by action type, paginated results, timestamp display

### Services & Infrastructure
- **AuthService** — Login/logout/session state management, token + username in localStorage
- **UserCharmService** — Fetches and rerolls the authenticated user's lucky charm from `/api/auth/charm`
- **AuthInterceptor** — Functional HTTP interceptor attaches Bearer token to all outgoing requests
- **AuthGuard** — Functional route guard, redirects unauthenticated users to `/login`
- **Dev Proxy** — `/api` requests proxied to `localhost:5000` during development

---

## CI/CD — GitHub Actions

Demo integration of [agentic-code-reviewers](https://github.com/jpolvora/agentic-code-reviewers): remote `run.sh` from the `release` branch, detached-HEAD workaround (`git checkout -B` on the PR head ref), and a cooperative **review → auto-fix → review** loop.

### Self-healing loop

```
PR opened/updated → Code Review → Auto Fix → push → Code Review → …
```

Auto-fix runs after each code-review workflow completes. When it commits and pushes, the next PR `synchronize` event re-triggers review. A PAT (`AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN`) is required for push, thread resolution, and reliable workflow chaining — the default `GITHUB_TOKEN` cannot re-fire workflows on bot pushes.

### Agentic Code Review (`.github/workflows/code-review.yml`)

| | |
|---|---|
| **Triggers** | `pull_request` → `main` — `opened`, `synchronize`, `reopened` |
| **Engine** | `cursor-sdk` / `composer-2.5` |
| **Runner** | `curl` + `run.sh@release` (`--gh`, `--pr-id`, source/target branch refs) |
| **Scope** | Backend (C#) and frontend (TypeScript/Angular) diffs |
| **Outcome** | Publishes review threads; fails if active threads remain (triggers auto-fix) |

### Agentic Auto Fix (`.github/workflows/auto-fix.yml`)

| | |
|---|---|
| **Triggers** | `workflow_run` after **Agentic Code Review** completes; `workflow_dispatch` (PR number) |
| **Condition** | Runs when the review workflow ends in `success` or `failure` |
| **Engine** | `opencode` / `opencode-go/deepseek-v4-flash` |
| **Concurrency** | One job per PR (`cancel-in-progress: false`) |
| **Behavior** | Skips when no active threads; otherwise `--auto-fix` — reads open threads, applies fixes, validates build, resolves threads, pushes to the PR branch |

PR metadata is resolved from `workflow_run.pull_requests` or, as fallback, `gh api …/commits/{sha}/pulls`.

### Repository secrets

Synced from `.env` via `gh secret set` (or GitHub MCP when configured):

| Secret | Used by |
|--------|---------|
| `CURSOR_API_KEY` | Code review (`cursor-sdk`) |
| `OPENCODE_API_KEY` | Auto-fix (`opencode`) |
| `AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN` | Both workflows — publish/resolve PR threads, push fixes, workflow dispatch |

---

## MCP (IDE)

Configured in `.cursor/mcp.json`:

| Server | Purpose |
|--------|---------|
| **github** | GitHub Copilot MCP (`api.githubcopilot.com`) — auth via `GITHUB_TOKEN` env |
| **azure-devops** | Azure DevOps MCP — org from `AZURE_DEVOPS_ORG`, loads `.env` via `envFile` |

---

## Agent Skills (IDE-level)

| Skill | Purpose |
|-------|---------|
| `code-review-self` | Read-only AI code review within the IDE, two-phase analysis, JSON output |
| `fix-pr` | PR triage + fix + commit + push workflow with user confirmation gate |
| `solve-pr` | Cooperative thread resolution loop (fix → commit → resolve → push) |
| `local-code-review` | Windows local dry-run review via PowerShell + Git Bash |
| `megabrain` | Multi-round review tracker with chronological thread IDs |
