# Features

## Backend — ASP.NET Core 10.0 Web API

### Authentication & Session Management
- **Login** (`POST /api/auth/login`) — Username/password authentication with salted SHA-256 hashing, returns Bearer token (7-day expiry)
- **Logout** (`POST /api/auth/logout`) — Invalidates session token in SQLite
- **Current User** (`GET /api/auth/me`) — Returns authenticated user's username
- **System Stats** (`GET /api/auth/stats`) — Returns total users, active sessions, server time
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
- **Dashboard** (`/dashboard`) — Welcome message, system stats display (total users, active sessions, server time), logout
- **Profile** (`/profile`) — Update username form, pre-populated current username, success/error banners
- **Documents** (`/documents`) — Document listing with file metadata, click-to-select download, blob-based file download, WCAG-accessible inputs
- **Audit Log** (`/audit`) — Full audit trail table with action badges, filter by action type, paginated results, timestamp display

### Services & Infrastructure
- **AuthService** — Login/logout/session state management, token + username in localStorage
- **AuthInterceptor** — Functional HTTP interceptor attaches Bearer token to all outgoing requests
- **AuthGuard** — Functional route guard, redirects unauthenticated users to `/login`
- **Dev Proxy** — `/api` requests proxied to `localhost:5000` during development

---

## CI/CD — GitHub Actions

### Agentic Code Review (`code-review.yml`)
- **Triggers** on PR events: `opened`, `synchronize`, `reopened`
- Runs multi-agent AI code reviewer (`agentic-code-reviewers`) with `cursor-sdk` engine
- Blocks merge if unresolved review threads remain
- Analyzes both backend (C#) and frontend (TypeScript/Angular) diffs

### Agentic Auto Fix (`auto-fix.yml`)
- **Triggers** on completion of code review workflow
- Automatically fixes and resolves code review threads
- Uses `opencode` engine for autonomous fixes
- Commits and pushes fixes back to the PR branch

---

## Agent Skills (IDE-level)

| Skill | Purpose |
|-------|---------|
| `code-review-self` | Read-only AI code review within the IDE, two-phase analysis, JSON output |
| `fix-pr` | PR triage + fix + commit + push workflow with user confirmation gate |
| `solve-pr` | Cooperative thread resolution loop (fix → commit → resolve → push) |
| `local-code-review` | Windows local dry-run review via PowerShell + Git Bash |
| `megabrain` | Multi-round review tracker with chronological thread IDs |
