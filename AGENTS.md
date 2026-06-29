# Developer Agents Guidelines (AGENTS.md)

This file contains crucial instructions, layout descriptions, and conventions for AI developer agents working on this project.

---

## Invariant Behavior

- **English-Only Policy:** Use English only when writing prompts, code, documentation, and LLM communication in this project. Translate prompts from any language to English before generating the response. All agent outputs and comments must be strictly in English.
- **Only implement what is explicitly requested.** On any ambiguity or design fork, stop and ask.
- **Be critical, not compliant.** Challenge assumptions; reject architecturally unsound suggestions with a technical rationale.
- **Simplicity first.** Minimal changes, no workarounds, no over-engineering.
- **Think more, write less.** Prefer elegant solutions over verbose ones. Less code is better: eliminate redundancy, abstract only when two or more real cases exist.
- **No token maxxing.** Responses and diffs must be concise. Avoid comments that merely paraphrase code, unnecessary explanatory prose, and mechanically generated boilerplate.
- **Tests are a contract, not optional.** Every feature or fix must ship with tests covering the happy path and relevant edge cases. Never delete existing tests unless they are dead code or unreachable.
- **Documentation stays in sync with code.** After every implementation (feature, fix, or behavior change), update **all affected documentation** in the same change set — do not merge code that drifts from docs. Minimum checklist when touching runner behavior:
  - **`AGENTS.md`** — architecture, env vars, gate rules, skills routing
  - **`README.md`** — user-facing features, CLI, workflows, env tables
  - **`.env.example`** — when env vars are added, renamed, or defaults change
  - Run **`npm test`** (and `npm run test:seed` when review/detection behavior changes) before considering the task done.
- **No Legacy Compatibility by Default:** You do not need to maintain backward compatibility with legacy versions or formats. In case of important implementation corrections or refactorings, inform the developer and feel free to introduce breaking changes by default (as the product is in active development). Only implement backward compatibility treatments if the programmer explicitly specifies that they want to maintain it.
- **Decompose before executing.** Break large tasks into independent subtasks. When possible, parallelize with subagents sharing only the minimum necessary context — keep context windows small.


## 🚀 Repository Purpose
This is a **demo repository** showcasing the integration of [agentic-code-reviewers](https://github.com/jpolvora/agentic-code-reviewers) — a multi-agent, AI-powered PR reviewer — running in a CI/CD pipeline under **GitHub Actions**.

---

## 📁 Project Structure

*   **[.github/workflows/code-review.yml](file:///.github/workflows/code-review.yml)**: PR-triggered AI code review via remote `run.sh` (`release` branch).
*   **[.github/workflows/auto-fix.yml](file:///.github/workflows/auto-fix.yml)**: Post-review auto-fix pipeline (`workflow_run` after code review).
*   **[backend/](file:///backend/)**: ASP.NET Core (C#) Web API application.
*   **[frontend/](file:///frontend/)**: Angular (TypeScript) client application.

---

## 🛠️ Agentic Code Reviewers Pipeline Rules

If you are asked to configure, debug, or modify the code review pipeline, adhere to the following logic:

### 1. Branch Checkout & CI Detached HEAD Workaround
GitHub Actions checkout defaults to a detached HEAD on a merge commit. Since `agentic-code-reviewers` performs a git fetch for remote branches in CI mode, PRs from forks will fail due to missing references on `origin`.
*   **Always** ensure the local branch matches the PR head branch prior to running `agentic-code-reviewers`:
    ```bash
    git checkout -B "${{ github.head_ref }}"
    ```
*   This triggers **Local Mode** in `agentic-code-reviewers`, using the checked-out HEAD directly for diff analysis and preventing network fetch errors.

### 2. Execution Flags
*   We run the reviewer remotely via the **`release`** branch (compiled artifacts aligned with `run.sh`):
    ```bash
    curl -fsSL https://raw.githubusercontent.com/jpolvora/agentic-code-reviewers/release/run.sh | bash -s -- \
      --engine cursor-sdk --model composer-2.5 \
      --gh \
      --pr-id "${{ github.event.pull_request.number }}" \
      --source-branch "refs/heads/${{ github.head_ref }}" \
      --target-branch "refs/heads/${{ github.event.pull_request.base.ref }}"
    ```
*   Supply `--gh`, `--pr-id`, `--source-branch`, and `--target-branch` explicitly.
*   Prefer `AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN` (PAT) for thread resolution; fall back to `github.token` for publishing only.
*   After review, the workflow fails if active bot threads remain — triggering the auto-fix pipeline.

---

## 💡 Code & Tech Stack Guidelines

*   **Backend (C#)**: Follow standard .NET Core web API controller-service architectures. Avoid modifying files outside the scope of review/debugging unless explicitly requested.
*   **Frontend (Angular)**: Follow Angular best practices. Keep CSS/styles isolated or use tailwind/vanilla conventions as described in the workspace settings.
