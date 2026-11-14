# Legal texts: changelog

Every change to the Terms of Use or the privacy notice gets a new dated copy in this folder (`terms-YYYY-MM-DD.md`, `privacy-YYYY-MM-DD.md`) and a line here. The version string is the date. The same string is a constant in the app (`src/web/src/core/legal/legalInfo.ts`, and the backend constant from Phase 19 Step 2). A test pins the text, so a change needs a deliberate edit of the test.

Not legal advice, not checked by a lawyer. Research: `docs/reports/2026-11-04-phase-19-legal-research.md`. Accepted open questions: `docs/DECISIONS.md`, "Phase 19".

| Version | What changed | Kind of change |
|---|---|---|
| 2026-11-04 | First Terms of Use and a full rewrite of the privacy notice (controller and contact, purposes, recipients, transfers, retention, rights, deletion, download, browser storage, age 16). | First version. Everyone who signed up before it sees the "terms changed" notice once. |

## How to publish a change
1. Edit the text in `en.json` and `mk.json` and the pinned test.
2. Change `legalVersion` (frontend) and the backend constant to the new date.
3. Save a dated copy here and add a line to the table.
4. Decide: small change (thin notice with one OK tap) or material change (new kind of data, fees, new purpose: a blocking sheet people must answer). Write the choice in the table.
