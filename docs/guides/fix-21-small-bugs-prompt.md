# Prompt for the next session: Fix 21, small bugs people hit

Paste everything below the line into a new session.

---

We are continuing Kvit. Read `docs/STATUS.md` and `docs/ROADMAP.md` first, then the "2026-11-14: Render wake round trip" section of `docs/DECISIONS.md`.

## Where things stand (written 2026-11-14)

- Phase 19 is merged (PR #23). Right after it we found that the live app was down whenever the Render server slept: a sleeping server answers 429 to everything that comes through Cloudflare and wakes only for a real person's page visit to `https://kvit-mk-api.onrender.com`.
- The fix is on the branch `fix/20-render-wake`: the app goes through Render's wake page and back by itself when it is opened, shows a "Wake the server" bar later in a session, and the cleanup runs after the first signed-in visit instead of on a schedule. It passed every local check (backend 7450, frontend 15720). It has never been seen working on the real site.
- I (Filip) was about to commit, push and merge that branch and then do the live check listed in `docs/STATUS.md` under "Next step".

## First, find out what happened

1. Check with git and `gh` whether `fix/20-render-wake` is merged into `main` and whether the CI run on `main` is green (it also deploys to Render). Don't trust this prompt for that; look.
2. Ask me what I saw in the live check, if I haven't said. If something failed (a "verify you are human" box, no return to Kvit, the old "Something went wrong" screen), that comes before anything else: diagnose it with me before starting new work.
3. If the fix is not merged yet, stop and tell me; the new branch must start from a `main` that contains it.

One rule while I am doing the live check: nothing may be merged into `main` and nobody may call the live site or the Render address, because any request or deploy wakes the server and spoils my 20-minute waits. Local work and pushing a branch are fine. Don't probe the live site yourself unless I ask.

## Then: the new branch

Make `fix/21-small-bugs` from the updated `main`. The work is five independent bugs from `docs/BACKLOG.md`; none needs a database change or a decision from me:

1. **Editing an old expense is refused** (`MEMBER_NOT_FOUND`) when its split still lists someone who was removed or left, so even a title fix is blocked. Wanted: people already in that expense's split are allowed when their typed values do not change. (BACKLOG: "Editing an old expense whose split still lists a person...")
2. **The Add expense screen crashes** when a member is removed in another session while it is open (the split draft keeps the removed person). Wanted: drop people who are no longer members from the draft when the members list changes, keeping what was typed. (BACKLOG: "Found by the Phase 8 code review", last item)
3. **The browser's Back button after joining opens "Create account" while signed in.** Wanted: `/signup`, `/login` and `/welcome` send a signed-in person away, and the in-app Back button and the Sign up / Log in links replace history instead of adding to it. (BACKLOG: "Found in the Phase 7 real-browser check")
4. **Long names without spaces stretch the page**: the Members page and Home's "Hi, name" greeting. (BACKLOG: "Phase 12b leftovers" (a))
5. **Cleanup retry**: after a failed cleanup run, a person already counted today does not start it again, while the log message says the next Active event retries. Wanted: either start the cleanup for already-counted people too (cheap) or correct the message; propose which. (BACKLOG: "Render asleep answers 429", the tester's note)

Before the coder: read the code for each bug, tell me in plain words what you will change for each one, give your own suggestions, and wait for my go. Tests for each, then the tester's full run, then a browser check at 360 px for 2, 3 and 4.

## One question to ask me before you start

The privacy notice does not name GitHub, where the weekly encrypted backup is stored (BACKLOG, "Privacy notice must name GitHub"). Fixing it changes the notice text, so everyone sees the "Terms changed" pill once more. Ask me whether to put it on this branch or to batch it with other wording changes (the "after 30 days" wording is a candidate for the same batch, see `docs/STATUS.md`).

## Still mine, not yours

Deleting `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub, running the Backup workflow once with the practice restore, turning on GitHub private vulnerability reporting, and Part E of `docs/guides/phase-19-legal.md`. Remind me of whichever are still open when we wrap up.
