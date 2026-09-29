# Setting up your PC for Kvit

Everything here is free. Do the steps in order. Each one ends with a **check** so you know it worked.
You may already have some of these from DocuMind or Book Scanner. Run the check first, and if it works, skip that step.

**How to run a check:** open **PowerShell** (press the Windows key, type `powershell`, press Enter), type the command, press Enter.

> Written 2026-09-25. Download pages change, so if a screen looks different from what's described, go with what the official site says. Items marked (NOT VERIFIED) weren't confirmed on the official site today.

---

## 1. Git (saves the history of your code)
You almost certainly have this already from DocuMind.
- **Check:** `git --version` shows something like `git version 2.x`. If it does, skip to step 2.
- If not: go to **git-scm.com** → Download for Windows → run the installer → click **Next** on every screen (the defaults are fine).
- Close and reopen PowerShell, then run the check again.

## 2. VS Code (the editor)
You already use it.
- **Check:** it opens. Skip to step 3.

## 3. VS Code extensions (add-ons)
In VS Code, click the **Extensions** icon on the left (four little squares), search for each name below and click **Install**:

| Extension | What it's for |
|---|---|
| **C# Dev Kit** (by Microsoft) | Writing and running C# / .NET. It also installs the "C#" extension. Free for individuals (NOT VERIFIED for the current licence text) |
| **Oxc** (by oxc) | Points out mistakes in the React code. Kvit's template uses **oxlint** instead of ESLint (changed 2026-09-25 after checking the Vite template) |
| **Prettier – Code formatter** | Keeps the React code neatly formatted |
| **REST Client** (by Huachao Mao) | Lets you click "Send Request" in `.http` files to test the backend (this replaces Postman) |
| **Docker** (by Microsoft) | Shows your running containers (optional, but handy) |
| **TypeScript 7** (by Microsoft) | Renamed from "TypeScript (Native Preview)" (Microsoft's marketplace listing, seen 2026-09-26). Phase 2 kept TypeScript 7, and VS Code's own built-in TypeScript is older, so this extension is needed for correct type-checking in `.ts`/`.tsx` files |

- **Check:** each one shows "Installed" or a gear icon instead of the Install button.
- **For the TypeScript 7 extension only, one extra step:** open the Command Palette (Ctrl+Shift+P) → run **"TypeScript: Enable TypeScript 7 Language Server"** (renamed from "TypeScript Native Preview: Enable (Experimental)", seen 2026-09-26). Installing it alone isn't enough — without this step VS Code keeps using its own older built-in TypeScript.

## 4. .NET 10 SDK (to build and run the backend)
- Go to **dotnet.microsoft.com/download** → pick **.NET 10** (marked LTS) → under **SDK**, download the **Windows x64 installer** → run it → **Install**.
- Pick the **SDK**, not "Runtime". The SDK is what lets you build code.
- Close and reopen PowerShell.
- **Check:** `dotnet --version` shows a number starting with `10.`.

## 5. Node.js (to build and run the React frontend)
You might already have it from Book Scanner.
- **Check:** `node -v` shows `v22.12` or higher (e.g. `v22.x` or `v24.x`). If so, skip to step 6. Vite needs 20.19+ or 22.12+.
- If not: go to **nodejs.org** → download the version marked **LTS** → run the installer → click **Next** on every screen. Leave "Add to PATH" ticked.
- Close and reopen PowerShell.
- **Check:** `node -v` and `npm -v` both show numbers.

## 6. WSL 2 (needed by Docker)
WSL is a small Linux that runs inside Windows. Docker uses it.
- Open **PowerShell as administrator**: press the Windows key, type `powershell`, **right-click** it → **Run as administrator** → Yes.
- Run: `wsl --install`
- **Restart your PC** when it asks.
- **Check:** after the restart, `wsl --status` in normal PowerShell shows version 2.

## 7. Docker Desktop (runs a real database for the tests)
The backend tests start a real PostgreSQL database inside Docker, so they test the real thing.
- Go to **docker.com/products/docker-desktop** → **Download for Windows (AMD64)** → run the installer → keep **"Use WSL 2"** ticked → finish → restart if it asks.
- Open **Docker Desktop** from the Start menu. Accept the terms (free for personal use and small businesses, NOT VERIFIED for the current terms). You can skip signing in.
- Wait until the bottom-left corner says **"Engine running"** (green).
- **Check:** in PowerShell run `docker run hello-world`. You should see **"Hello from Docker!"**.
- **Tip:** Docker Desktop uses a lot of memory. Close it when you're not working on Kvit, and open it again before running tests.

## 8. Final check (everything at once)
Paste this into PowerShell:
```
git --version; dotnet --version; node -v; npm -v; docker --version
```
You should see five lines of version numbers and no red errors. Take a screenshot of it. If something is red, send the screenshot in the Claude session.

---

## Accounts you'll need later (don't create them yet)
The **free-hosting setup guide** (written in the building session) walks through each of these when it's time. None of them needs a card.
- **GitHub** (you already have one)
- **Neon** (database)
- **Render** (runs the backend online)
- **Cloudflare** (runs the website online)
- **Google Cloud** (only to create the "Sign in with Google" button; no billing)

## If something goes wrong
- **"Command not recognised":** close **all** PowerShell and VS Code windows and open them again. New installs only show up in newly opened windows.
- **Docker says WSL is missing or old:** run `wsl --update` in PowerShell as administrator, then restart your PC.
- **Still stuck:** screenshot the error and paste it into the Claude session. Don't guess-fix it.
