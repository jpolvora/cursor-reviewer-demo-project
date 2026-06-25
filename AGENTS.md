# Developer Agents Guidelines (AGENTS.md)

This file contains crucial instructions, layout descriptions, and conventions for AI developer agents working on this project.

---

## 🚀 Repository Purpose
This is a **demo repository** showcasing the integration of [cursor-reviewer](https://github.com/jpolvora/cursor-reviewer) — an agentic, AI-powered PR reviewer built with `@cursor/sdk` — running in a CI/CD pipeline under **GitHub Actions**.

---

## 📁 Project Structure

*   **[.github/workflows/review.yml](file:///.github/workflows/review.yml)**: The GitHub Actions workflow executing `cursor-reviewer` remotely.
*   **[backend/](file:///backend/)**: ASP.NET Core (C#) Web API application.
*   **[frontend/](file:///frontend/)**: Angular (TypeScript) client application.

---

## 🛠️ Cursor Reviewer Pipeline Rules

If you are asked to configure, debug, or modify the code review pipeline, adhere to the following logic:

### 1. Branch Checkout & CI Detached HEAD Workaround
GitHub Actions checkout defaults to a detached HEAD on a merge commit. Since `cursor-reviewer` performs a git fetch for remote branches in CI mode, PRs from forks will fail due to missing references on `origin`.
*   **Always** ensure the local branch matches the PR head branch prior to running `cursor-reviewer`:
    ```bash
    git checkout -B "${{ github.head_ref }}"
    ```
*   This triggers **Local Mode** in `cursor-reviewer`, using the checked-out HEAD directly for diff analysis and preventing network fetch errors.

### 2. Execution Flags
*   We run the reviewer remotely via:
    ```bash
    curl -fsSL https://raw.githubusercontent.com/jpolvora/cursor-reviewer/main/run.sh | bash -s -- \
      --source-branch "refs/heads/${{ github.head_ref }}" \
      --target-branch "refs/heads/${{ github.base_ref }}"
    ```
*   Ensure that `--source-branch` and `--target-branch` parameters are supplied explicitly.

---

## 💡 Code & Tech Stack Guidelines

*   **Backend (C#)**: Follow standard .NET Core web API controller-service architectures. Avoid modifying files outside the scope of review/debugging unless explicitly requested.
*   **Frontend (Angular)**: Follow Angular best practices. Keep CSS/styles isolated or use tailwind/vanilla conventions as described in the workspace settings.
