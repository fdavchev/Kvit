# Starting the Kvit repository (your steps)

Two parts:
- **Part 1: now, about 5 minutes.** After this, Claude can start writing code.
- **Part 2: tonight.** Accounts and GitHub. No code needed for any of it.

Everything is typed in the **VS Code terminal**: in VS Code, open the menu **Terminal → New Terminal**. It opens already inside the Kvit folder.

---

# PART 1: now (so coding can start)

## 1. Turn the folder into a git repository
Type:
```
git init -b main
```
- **You should see:** `Initialized empty Git repository in C:/Users/Davchev/Projects/Kvit/.git/`
- `-b main` names the first branch `main`. Without it, your git would call it `master`, and the hosting guide expects `main`.

## 2. Check what will be saved
```
git status
```
- **You should see:** in red, under "Untracked files": `CLAUDE.md`, `README.md`, `START-HERE-PROMPT.md` and `docs/`.
- Nothing else. If you see any other file, stop and ask in the Claude session before step 3.

## 3. Save the planning documents as the first commit
Run these two commands, one after the other:
```
git add .
```
```
git commit -m "Add Kvit planning documents" -m "Adds the product decisions, architecture, data model, screen list, build roadmap, setup guides and research reports written during planning. No code yet."
```
- **You should see:** a line like `[main (root-commit) 1a2b3c4] Add Kvit planning documents` and "N files changed".
- The first `-m` is the subject, the second is the body.

## 4. Make the branch for the first phase
```
git switch -c feat/01-backend-skeleton
```
- **You should see:** `Switched to a new branch 'feat/01-backend-skeleton'`
- **Check:** `git branch` shows `* feat/01-backend-skeleton` (the star marks where you are).

## 5. Open Docker Desktop
- Start menu → **Docker Desktop**. Wait for **"Engine running"** in the bottom-left corner.
- Phase 1 checks that the backend's Docker image builds. Without Docker running, that one check gets reported as NOT VERIFIED instead.
- You can close it again when you're done for the day.

## 6. Tell Claude "go"
Say it in the Claude session, together with your answers to the open questions (end of `docs/DATA-MODEL.md`), or just "go with your suggestions".

---

# PART 2: tonight (accounts; no code needed)

## A. The hosting accounts
Follow **`docs/guides/free-hosting-setup.md`, Part A only** (A1 Neon, A2 Render, A3 Cloudflare, A4 Google Cloud).
- Keep the connection strings in your private notes file **outside** the Kvit folder (e.g. `Documents\kvit-secrets.txt`).
- **Never** paste them into the Claude chat.
- **Don't do Part B yet.** Part B needs the code from Phase 5.

## B. The GitHub repository
1. On github.com: **+** (top right) → **New repository**
   - **Name:** `kvit`
   - **Description:** *Split shared expenses and track your personal budget, in MKD and EUR, in English and Macedonian. Built with .NET 10, React and TypeScript, hosted for free.*
   - **Public**
   - **Don't** add a README (the folder already has one), leave .gitignore on **None** (the first code phase adds it) and license on **None** (the README says "All rights reserved"; see `DECISIONS.md`).
   - **Create repository**.
   - Optional, afterwards: click the ⚙️ next to **About** and add topics: `expense-splitter`, `budget`, `dotnet`, `aspnet-core`, `ef-core`, `postgresql`, `react`, `typescript`, `vite`, `docker`.
2. **Turn on push protection** (it blocks uploading a password by accident): the repository → **Settings** → **Advanced Security** (or "Code security") → **Push protection** → **Enable**.
3. Connect your folder to it. In the VS Code terminal (Filip's repository is `https://github.com/fdavchev/Kvit`, created 2026-09-25):
   ```
   git remote add origin https://github.com/fdavchev/Kvit.git
   ```
   ```
   git push -u origin main
   ```
   - **You should see:** a few lines ending with `branch 'main' set up to track 'origin/main'`.
   - **Check:** refresh the GitHub page. You see `docs/`, `CLAUDE.md` and `START-HERE-PROMPT.md`.
4. When Phase 1 is finished and you've committed it, push the branch the same way:
   ```
   git push -u origin feat/01-backend-skeleton
   ```
   GitHub then offers **Compare & pull request**. The PR text comes from the Claude session.

## If something goes wrong
| What you see | What to do |
|---|---|
| `fatal: not a git repository` | You're in the wrong folder. The terminal line should end with `\Kvit>`. Open the Kvit folder in VS Code again |
| `Author identity unknown` | Run `git config --global user.email "your@email"` once, then the commit again |
| `remote origin already exists` | The connection is already made. Skip to `git push` |
| The push asks you to log in | A browser window opens; log in to GitHub there |
| `push declined due to repository rule violations` / "secret detected" | Push protection found something that looks like a password. Don't force it; ask in the Claude session |
