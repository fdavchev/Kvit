# Status: Kvit

_Last updated: 2026-11-14 (Phase 19 merged as PR #23; the live app turned out to be down whenever Render sleeps; the fix is built and tested on `fix/20-render-wake`, not committed; waiting for Filip to commit, push, merge and do the live check)_

## Where we stopped
- Branch: `fix/20-render-wake` (made from `main` = `624dbea`, the Phase 19 merge). Nothing committed yet: the whole fix plus the docs edits since the merge are in the working folder.
- What the fix does: decisions in `DECISIONS.md` ("2026-11-14: Render wake round trip"); commit and PR texts in `docs/guides/fix-20-pr-text.md`.
- Phase 19 map: report `docs/reports/2026-11-09-phase-19-legal.md`; browser pass `docs/reports/2026-11-09-phase-19-browser-pass.md`; Filip's guide `docs/guides/phase-19-legal.md`; research `docs/reports/2026-11-04-phase-19-legal-research.md`.

## Next step
Filip:
1. Commit, push, open the PR with the texts in `docs/guides/fix-20-pr-text.md`, wait for green CI, merge, wait for the Render deploy.
2. The live check (only the real site can prove the fix):
   - Leave Kvit alone for 20 minutes, then open `https://kvit-mk.pages.dev` on the phone and on the PC. Expected: Render's black "waking up" page for about 30 seconds, then back on the same Kvit screen.
   - Leave a Kvit tab open for 20 minutes, then tap something. Expected: a bar "The server is asleep. · Wake the server" at the top; the button does the same trip.
   - Add an expense while the bar shows. Expected: it shows as waiting and is sent after the trip.
   - In the Render log after the first signed-in visit: a line "The daily cleanup of … finished".
3. Delete `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub (no longer used).
4. Still open from Phase 19 (`docs/guides/phase-19-legal.md`): run the **Backup** workflow once, the practice restore, **Private vulnerability reporting** on GitHub (checked 2026-11-14: off), and Part E.

## Blockers and open questions
- **Live app is down whenever Render sleeps until `fix/20-render-wake` is merged and deployed.** Until then it works for about 15 minutes after someone opens `https://kvit-mk-api.onrender.com/health` in a browser.
- Not known: whether every person passes Render's robot check without a tap (Filip's browser did), how an installed home-screen Kvit and iOS Safari behave on the trip, and whether Render changes this again (nothing official was found; the status page shows no incident).
- The privacy page and the delete sheet say "after 30 days"; the cleanup now runs on the first signed-in visit after that day, so with no visitors it is later. Texts not changed; Filip decides whether to reword.
- A failed cleanup run is tried again only on another person's first visit that day or on the next day, while the log message says "the next Active usage event" (BACKLOG).
- Open from Step 11: old "active people" numbers drop after 12 months (not decided, not built; first cut about 2027-10).
- Waiting for Filip: Phase 18 leftovers (light theme peach, Totals bars); the Macedonian drafts, now also «Серверот спие.» and «Разбуди го серверот».
- The privacy notice does not name GitHub yet, and the weekly backup is live (BACKLOG).
- Docker Desktop must be open for the backend tests (ask Filip, never work around it).
- Local database has throwaway `p19*@example.com` accounts; `p19admin1794216234@example.com` has the Admin role. Harmless.

## Verification state
On `fix/20-render-wake` (working folder, 2026-11-14):
- VERIFIED by automated test (tester, after the last code change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 7450/7450; `dotnet ef migrations has-pending-model-changes` clean; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 15720/15720; `npm run check:licenses` ok; `docker build` of the API image succeeds. The tester also read the new tests: no `returnTo` value can send `/wake` to another site; small test gaps are in BACKLOG.
- VERIFIED by live run (before the fix, on the real site): the failure itself (429 through Cloudflare for minutes; the Cleanup run 43602105291 failed on it; a background call, a hidden frame and an automated browser visit woke nothing; Filip's own browser visit to the Render address woke it in about 30 seconds).
- NOT VERIFIED: the fix on the real site (the whole list under "Next step" 2), the new bar in a real browser at 360 px, the Macedonian texts.

On `main` = `624dbea` (2026-11-14):
- VERIFIED by automated test: backend 7432/7432, frontend 15645/15645, build, lint, licences.
- VERIFIED by live run: CI on `main` green (run 43595025514) including the Neon migrations and the Render deploy; right after the deploy the site answered `/api/health` 200, `/`, `/terms`, `/privacy`, `security.txt` and `/third-party-licenses.txt` (text/plain, so the Cloudflare build runs the licence generator) 200, and the signed-in routes 401 without a login.
- NOT VERIFIED: the Backup workflow (never run), the real Neon dump and restore, the Phase 19 screens signed in on the live site, a real phone, iOS Safari, Firefox, a screen reader, the terms pill fix in a browser.
- Known flaky tests under load (pass alone; in BACKLOG): `GroupScreenMarkPaid.test.tsx` "queues the payment…", `JoinScreen.test.tsx` "goes straight to the group…".
