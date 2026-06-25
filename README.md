# Cursor Reviewer Demo Project

This is a demonstration project showcasing how to integrate **[cursor-reviewer](https://github.com/jpolvora/cursor-reviewer)** remotely within a **GitHub Actions** CI/CD pipeline.

The repository simulates a typical fullstack application with a **C# ASP.NET Core** backend and an **Angular** frontend.

---

## 🚀 How it Works

The GitHub Actions workflow [.github/workflows/review.yml](file:///.github/workflows/review.yml) triggers on pull requests targeting the `main` branch. 

When triggered, it:
1. Performs a full history checkout (`fetch-depth: 0`).
2. Creates and checks out a local branch tracking the PR head branch to support fork-based PRs without authentication/fetch failures.
3. Fetches and runs the remote execution script `run.sh` from the `cursor-reviewer` repository.
4. Performs an agentic, two-phase AI review using `@cursor/sdk` over the files changed in the PR.
5. Posts actionable review threads directly onto the GitHub Pull Request.

---

## 🛠️ GitHub Actions Setup

To enable this review pipeline in your repository:

### 1. Configure Secrets
Ensure the following secret is configured in your GitHub repository (**Settings > Secrets and variables > Actions**):
*   `CURSOR_API_KEY`: Your Cursor API Integration Key.

*(The `GITHUB_TOKEN` is automatically provided by GitHub Actions to authenticate PR reviews/comments).*

### 2. Workflow Permissions
The workflow requires permissions to write comments on the pull request. This is configured in the job definitions:
```yaml
permissions:
  contents: read
  pull-requests: write
```

---

## 📁 Repository Structure

*   **`backend/`**: Simple C# Web API application.
*   **`frontend/`**: Simple Angular TypeScript client application.
*   **`.github/workflows/review.yml`**: GitHub Actions review pipeline configuration.
