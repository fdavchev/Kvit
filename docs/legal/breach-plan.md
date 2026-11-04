# Data breach plan

Written 2026-11-04. A breach means personal data was lost, changed, shown or taken by someone who should not have had it (a leaked secret, a hacked account, a wrong export, a lost backup passphrase together with a downloaded backup). The law Kvit follows by choice (GDPR level, and the North Macedonia data law) expects the Personal Data Protection Agency to be told **within 72 hours of becoming aware**, unless the risk to people is unlikely. Not legal advice, not checked by a lawyer. Whether the 72-hour duty applies to a hobby project is one of the accepted open questions (`docs/DECISIONS.md`, "Phase 19"); the plan assumes it does, because that is the safe way.

## First hour (contain)
1. Write down the time you became aware, and what you saw.
2. Stop the leak. Depending on the case:
   - A secret leaked (connection string, proxy secret, Google client secret, backup passphrase, login-key certificate): replace it in Render, Cloudflare and GitHub secret settings (`docs/guides/free-hosting-setup.md` shows where each one lives), redeploy, and if the login-key certificate leaked make a new one (`scripts/NewDataProtectionCertificate.cs`), which logs everyone out.
   - An account is being abused: suspend it with the Owner tools (Phase 19 Step 7b) and write the reason.
   - A wrong release shows data to the wrong people: roll Render back to the last good deploy.
3. Do not delete logs or evidence.

## Same day (assess)
- What data, whose, how many people, since when, and can the other side read it (an encrypted backup without its passphrase is not readable).
- Risk to people: money amounts and names of friends are low to medium; emails and password hashes are medium; guest names alone are low.
- Add a row to `breach-log.md` now, even if unsure.

## Within 72 hours (tell)
- **The Agency:** the Personal Data Protection Agency of North Macedonia. Look up the current notification form and address on the Agency's website and write them at the top of `breach-log.md` the first time. If people in the EU are affected, also check the authority of that country.
- **The people affected**, if the risk to them is high: write to them in plain words (what happened, what data, what you did, what they should do, `kvit.support@gmail.com`). Use the email on their account; for guests and plain names use the group owner.
- If you cannot tell the Agency in time, tell it anyway and say why it is late.

## After (fix and learn)
- Fix the cause, add a test if it is code, write what changed in `docs/DECISIONS.md`.
- Complete the log row: cause, number of people, who was told and when, what changed.
- If the Terms or privacy notice become wrong, publish a new version (`CHANGELOG.md`).
