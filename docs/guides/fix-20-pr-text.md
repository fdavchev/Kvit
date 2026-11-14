# Fix 20: first-push texts (commit subject, PR title, PR description)

Branch: `fix/20-render-wake`. This file is the one place for these texts. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Wake the sleeping Render server through its own wake page and run the cleanup on the first visit of the day.

## PR title
Fix 20: wake the sleeping server and run the cleanup without a schedule

## PR description (paste this)

### Overview
Since about 2026-11-14 a sleeping Render server no longer wakes for requests that come through Cloudflare: it answers 429 until a person's own browser visits the Render address. The live app showed "Something went wrong" whenever the server was asleep, and the scheduled cleanup failed. This PR makes the app go through Render's wake page and come back by itself, and moves the cleanup from a schedule to the first signed-in visit.

### Scope
- The Cloudflare forwarder turns Render's "asleep" 429 into a 503 that carries the wake address; every other 429 passes through untouched.
- A new anonymous `GET /wake` on the API that only redirects back to the Kvit site (a fixed address, local paths only).
- The website: when it is opened and the server is asleep, it goes through Render's wake page and returns to the same screen; later in a session it shows a "The server is asleep · Wake the server" bar instead, so nothing typed is lost; no second automatic trip within 2 minutes.
- The cleanup runs in the background after the first recorded Active event; the Cleanup workflow, the internal endpoint and the `Cleanup__Secret` setting are removed.
- Docs: status and roadmap after the Phase 19 merge, the decisions, the Phase 19 guide.

### Database changes
None.

### Checklist
- [x] Forwarder: asleep answer becomes 503 with the wake address
- [x] API: `GET /wake`
- [x] Website: automatic trip at app open, the bar later, the 2-minute guard
- [x] Cleanup after the first signed-in visit; workflow, endpoint and secret removed
- [ ] Live check on the real site after the deploy (list in `docs/STATUS.md`)
- [ ] Delete `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub

## Later pushes (one-line commit subjects)
