# Phase 19 research: terms, privacy and "not getting sued"

_Written 2026-11-04. Four `researcher` runs (privacy law, Terms of Use, money and platform risks, in-app features). Nothing is built yet; this report is the base for Phase 19 in `ROADMAP.md` and `DECISIONS.md`._

## Read this first
- **I am not a lawyer and neither were the researchers.** Nothing here is legal advice. No document can make you impossible to sue; good documents make a claim weak, make clear who is responsible for what, and show you acted carefully.
- **Labels used below:** RESEARCHED-OFFICIAL (the researcher read the official text), RESEARCHED-SECONDARY (law-firm guide, blog, summary), UNCLEAR (no source settled it), LAWYER (a real lawyer should answer). Nothing here is VERIFIED by automated test or live run, because nothing is built.
- **The honest biggest points** are in the first section. The rest is detail.

## 1. The six things that matter most
1. **Kvit never touches money, and that is its best protection.** A ledger that only records "A owes B" and "A says they paid B outside the app" does not look like a payment service (RESEARCHED-SECONDARY, researcher's own reading of the EU payment directive and the Macedonian payment law; no regulator wrote about expense-splitting apps; LAWYER to confirm). Keep it that way: no kitty, no wallet, no "Pay now" button, no payment links, no bank linking, no interest or fees on debts.
2. **You are the "controller" of everyone's data**, even though your users are probably exempt for their own friends-and-family use. North Macedonia's data law (2020, GDPR-like) applies to you as an operator there; GDPR may also apply once strangers in the EU join (UNCLEAR, LAWYER). The sensible plan is to meet GDPR level anyway.
3. **The privacy page is missing required parts:** who you are plus a contact email, the legal reason for each use of data, how long data is kept, the people's rights (see, fix, delete, export), the right to complain to the regulator, and the US vendors with the safeguard used.
4. **Kvit needs real Terms of Use**, with the "I agree" line placed so a court would count it, and a record of which version each person accepted.
5. **Real account deletion and data export buttons** are the safest way to meet the one-month deadline without relying on you answering emails.
6. **Data loss is also a way to get complaints.** Neon's free plan can only restore the last 6 hours and you hold no copy of your own. A weekly encrypted backup (already in BACKLOG) should come into this phase.

## 2. Which laws touch Kvit
- **North Macedonia, Law on Personal Data Protection (2020, amended 2021, maybe 2025):** GDPR-like. The regulator is the Personal Data Protection Agency (AZLP). RESEARCHED-SECONDARY (CMS, Karanović, DLA Piper); the law text itself could not be read.
  - **Not needed for Kvit:** registering with the Agency, a data protection officer, formal records of processing (those are for 50+ staff or risky processing). A one-page record is still a good idea.
  - Fines for an individual controller are small (about EUR 100 to 500 per the guides); misuse of personal data can also be a crime.
  - Moving data to countries outside EU/EEA/NATO needs safeguards; moving to EU/EEA/NATO needs a 15-day notice to the Agency. Whether a one-person hobby app must file it is UNCLEAR (LAWYER).
- **GDPR** applies if you "target" people in the EU (Article 3(2)). Kvit has Macedonian and English, denars and euros, no EU marketing: weak targeting, but strangers joining makes it arguable. UNCLEAR.
- **Household exemption:** your users splitting a holiday are probably exempt. You, running the service, are not (GDPR recital 18). RESEARCHED-SECONDARY.
- **EU Digital Services Act (DSA):** every online service needs a public contact point (Art. 11); hosting services need a way to report content (Art. 16); a service from outside the EU offering to EU users may need an EU legal representative (Art. 13). Small enterprises are exempt from only the "online platform" section. Whether a private friends-and-family app is covered, or you count as an "enterprise", is UNCLEAR (LAWYER).
- **Macedonian Law on Electronic Commerce:** reportedly requires a service provider to show name, address and an email in an easy, permanent place. The article could not be read, and whether it covers a free hobby app run by an individual is UNCLEAR (LAWYER). **This is the one that could expose your home address.**
- **Payment law / AML / tax:** probably no licence or duty because no money flows; UNCLEAR for Macedonia (LAWYER).

## 3. What the privacy page must add (GDPR Art. 13 checklist)
Missing today: controller name and contact email; purposes and the legal basis for each (contract for running accounts and groups; legitimate interest for security, rate limits and anonymous counts); recipients (Cloudflare, Render, Neon, Google; NBRM only receives rate requests); transfers outside the EU and the safeguard; retention periods; rights (access, fix, erase, restrict, export, object); right to complain to AZLP (and an EU authority); whether giving data is required; "no automated decisions"; the source of data for people whose name someone else typed.
Already there: what is kept, passwords, Google, who sees what, cookies, where stored, deletion (in part).
- Add an "In short" box of about five lines on top, in EN and MK, then the full text. The law asks for "concise, plain language" (Art. 12).
- Mention the IndexedDB saved copy, the outbox queue, and the theme/language settings.
- Say what stays after deletion and that backups hold deleted data up to 6 hours.

## 4. Terms of Use: what goes in
Plain, short, EN + MK, saying which version wins. Clauses (RESEARCHED-SECONDARY, researcher's checklist; compared with Splitwise, Settle Up and Tricount):
- Minimum age (see question 6 below).
- You are responsible for your login. Guests have no email recovery.
- No harassment, illegal content, spam or impersonation; Kvit may remove content or suspend accounts.
- **Kvit is a notebook, not a bank.** It moves no money, does not guarantee anyone pays, "paid" and "confirmed" are what people say.
- Amounts, rounding and exchange rates may be wrong or late; no financial, tax or legal advice.
- No uptime promise, no promise that data is never lost, "as is".
- Disputes between members are between the members.
- Users keep ownership of what they type; Kvit may store and show it to their group.
- The code licence (if any) covers the code, not the hosted service or the name.
- Changes: how you tell people; termination; contact; if one clause is invalid the rest stays.
- **A sentence that nothing here removes rights the law gives you.** You cannot exclude liability for death or injury; intent and gross negligence usually cannot be excluded either (UNCLEAR for Macedonia). Skip an "indemnity" clause for consumers (likely unfair).
- Governing law: the default is Macedonian law and courts "without removing mandatory consumer rights of your country". EU consumers can usually still sue at home (UNCLEAR, LAWYER).
- **Making it binding with the fewest taps:** put "By continuing you agree to the Terms and Privacy Policy" in small grey text directly under the sign-up button, the Google button and the guest-join button. A US appeals court enforced exactly that layout (Meyer v. Uber, RESEARCHED-SECONDARY; not read in full). No tick box is needed. Save `terms_version` and `accepted_at` per user; do not save the IP. For small changes show a thin banner with one OK tap; for material changes (new data use, fees) a blocking sheet.
- **Templates:** Kyle Mitchell's "Plain Terms of Service" is public-domain (CC0). Copying another app's terms is a copyright risk and would not fit guests, plain-name members and two currencies.

## 5. Features the law pushes you to build
| Feature | Why | Taps | Size |
|---|---|---|---|
| Contact email on /privacy, /terms, security.txt | Required by data law, DSA and Google | 0 | S |
| Terms page EN + MK | Binds users, sets disclaimers | 0 | M |
| Agree line under sign-up, Google and guest buttons | Makes terms binding | 0 | S |
| Store terms version + date per user | Proof of what they accepted | 0 | S |
| Delete my account (Settings) | One-month erasure duty; Google's rules for apps | about 3 | M-L |
| Download my data (JSON) | Access and portability duty | 1 | M |
| Hint under the plain-name field | People named by others (Art. 14) | 0 | S |
| "Report" in the group menu (opens an email) | DSA notice route | 1 | S |
| Cleanup job (GitHub Actions cron + protected endpoint) | Makes the promised deletion dates true | 0 | M |
| Weekly encrypted database backup | Neon keeps only 6 hours | 0 | M |
| Third-party licence notices page, GPL check in CI | MIT/BSD/Apache need notices; avoid GPL/AGPL | 0 | M |
| SECURITY.md + security.txt | Basic security disclosure | 0 | S |
| Breach plan + breach log in `docs/legal/` | 72-hour duty to tell the regulator | 0 | S |
| `docs/legal/` with every dated version of the texts | Evidence of what you said when | 0 | S |
| Rates source line + disclaimer | The National Bank requires naming the source | 0 | S |
| Owner tools: suspend user, close group, action log | Acting on reports | 0 | M |
| Revocable invite links, "You were added by X. Not you? Leave." | Harassment protection | 0-1 | M |

## 6. Deleting an account: what the research changes in the BACKLOG plan
Your BACKLOG plan (30 days off, then the person stays as a plain-name member) is close. Changes:
- **Switch the account off the moment they tap Delete** (no sign-in, no use), and count the 30 days from that tap. Logging in again cancels it. Say "deleted within 30 days" on the privacy page. (GDPR says "without undue delay" and answer within one month; a cancellable cooling-off period is a reasoned reading, UNCLEAR.)
- **Offer the name choice:** "Keep my name" (default) or "Show as Former member". A name is personal data and no regulator text blesses keeping it. If they ask for "Former member", or send any erasure request, do that everywhere, including the activity feed.
- **Always removed:** email, login, Google link, password hash, personal budget, sessions, outbox copies, usage events tied to the account.
- **May stay:** the shared expense rows other people's balances depend on (reasoned position, no regulator decision found, LAWYER).
- Works for guests too (no email). Entry: Settings. Also describe how to delete on the privacy page so it is findable from outside the app.

## 7. Money and platform details
- **Safe wording:** "Kvit only keeps a record. It does not hold, send or receive money." Use "record" and "confirm", never "send", "transfer" or "collect". Reminders should be neutral, never threatening or repeated, and never add fees or interest.
- **Exchange rates (National Bank, NBRM):** copying its data is allowed if NBRM is named as the source, and you must not change the content. Show "Exchange rates: National Bank of the Republic of North Macedonia (NBRM), nbrm.mk" and "Rates are informational, may be delayed or wrong". Never call your own derived or cross rates "NBRM rates". No separate rule for automated fetching was found, so a short email to NBRM asking is wise.
- **Google sign-in:** Google requires an accurate privacy policy linked in the console. Showing your app name instead of the address needs a domain you own and verify, which the researcher believes is impossible on `pages.dev` (inference, not confirmed). That is why Google's window shows the address today (already accepted in Phase 6). A custom domain costs money and breaks free-forever, so it stays a decision for Filip.
- **Google script loads on page view** and sends the visitor's IP to Google before any tap. A pure-privacy fix is to load it only when the person taps "Continue with Google". That risks slowing the first tap (see question 8).
- **Render:** its docs say free services are for hobby projects and "Do not use them for production applications"; they sleep after 15 minutes and can be restarted at any time. Render has a DPA and is certified under the EU-US Data Privacy Framework (since Jan 2025). Frankfurt may be selectable; check the dashboard.
- **Neon:** free plan has 6 hours of restore history and no scheduled backups. A data-processing agreement is built into its terms (whether it reaches free plans is unconfirmed).
- **Cloudflare:** its DPA is part of the self-serve agreement, which covers free accounts. Free Pages Functions have a 100,000 requests per day cap; going over would break `/api/*`.
- **EU-US data framework:** upheld by the EU General Court in Sept 2025; an appeal (C-703/25 P) was filed. No result found as of today (UNCLEAR). Keep screenshots of each vendor's DPA page.
- **Open-source licences:** MIT/BSD/ISC/Apache need their licence text kept with copies you distribute, so the web app should have a licences page or file. Avoid GPL and AGPL in the shipped bundle. Tools named by the researcher (not checked as current): `license-checker-rseidelsohn`, `generate-license-file`, `rollup-plugin-license` for npm; `nuget-license` for NuGet. Fonts and icons were not checked (UNCLEAR).
- **Your own repository has no licence = all rights reserved.** Choose one (MIT is the simplest; AGPL stops closed hosted clones). The name «Квит» is not covered by any licence.
- **Trademark "Kvit" / «Квит»:** web searches found nothing, but that is not a real search. Search TMview and the Macedonian IP office yourself (the researcher had no way to run those databases).
- **Security disclosure:** `SECURITY.md` with a contact and GitHub private vulnerability reporting; `/.well-known/security.txt` per RFC 9116 (needs `Contact` and `Expires`, renew within a year).
- **Contact mailbox:** one dedicated address (a new Gmail is acceptable) for privacy, abuse, security and deletion requests.

## 8. Cookies and the saved copy
- The login cookie is "strictly necessary", so no cookie banner. A 90-day persistent cookie is more debatable than a session cookie (UNCLEAR); a "Stay signed in" opt-in or a shorter life is the safe alternative.
- The outbox is needed for the offline function; the instant-loading saved copy is a performance feature but local-only and cleared on log-out. Disclose both in the privacy page. Low risk.
- Theme and language setting: low risk, disclose.

## 9. Children
GDPR default age 16 (countries may lower to 13); North Macedonia 14 for online services (RESEARCHED-SECONDARY, article not seen); Splitwise says 13; Google about 13. Recommended lowest-tap approach: one grey line "Kvit is for people aged 16 and over" in the Terms and at sign-up, no tick box, no age field. Legal effect of a one-sided "16+" is UNCLEAR.

## 10. Retention (reasoned choices, no regulator number exists)
Deleted account: switched off at once, purged within 30 days. Soft-deleted expenses: removed 30 days after the Undo window. Usage events: monthly totals after 12 months. Server logs and IPs: 7 to 30 days. Backups: up to 6 hours on Neon, plus Filip's own weekly encrypted backup (needs a stated retention, e.g. 90 days). Inactive accounts: not decided (see question 10).

## 11. Personal liability and company
No source said whether an individual running a free app is personally liable (LAWYER). A sole proprietorship gives no shield (unlimited liability) and needs residence in North Macedonia. A limited company (d.o.o.) limits liability and gives a business address for the imprint, but brings accounting, filings and yearly cost, which conflicts with "free forever". No free insurance or shield exists. Cheap mitigations are the ones above: hold no money, collect little data, clear disclaimers, a working deletion process.

## 12. What a lawyer should answer (one short consultation could cover all)
1. Do you have to publish name and address (e-commerce law), or is an email enough?
2. Does the DSA or GDPR apply to a private friends-and-family app run from North Macedonia, and is an EU representative needed?
3. Is a pure debt ledger outside the payment-services law, consumer-credit rules and anti-money-laundering rules?
4. May shared expense rows and a plain name stay after a deletion request?
5. Does the "By continuing you agree" line bind users under Macedonian and EU consumer law, and is the liability limit and court clause valid?
6. Is the Agency's 15-day transfer notice needed?
7. Is a company worth it?

## 13. Not verified by the researchers
Macedonian law articles (imprint, language, unfair terms, children, 2025 amendments); the Render and Neon terms and acceptable-use pages, and whether their DPAs reach free plans; whether Render's free tier offers Frankfurt; what the Google sign-in script sets; Google's current minimum age and button rules; the data framework appeal; actual licences of Kvit's fonts, icons and npm/NuGet packages; trademark databases; whether the licence tools are current; how comparable apps handle consent flows.

## 14. Main sources
GDPR Articles 12, 13, 14, 17, 20, 33 (gdpr-info.eu); EDPB Guidelines 3/2018 and 2/2023; DSA Articles 11, 13, 14, 16, 19 (springlex.eu mirror); Google OAuth help pages (support.google.com/cloud/answer/13463073 and 13464321) and API Services User Data Policy; Google Play deletion policy (support.google.com/googleplay/android-developer/answer/13327111); CMS and DLA Piper guides for North Macedonia; Karanović news on the payment law; NBRM web information page (nbrm.mk/veb-info-en.nspx); Render (render.com/docs/free, /dpa, /docs/regions); Neon (neon.com/docs/introduction/plans, /restore-window, /dpa); Cloudflare DPA; Splitwise, Settle Up and Tricount terms; kemitchell.com Plain Terms of Service; RFC 9116. Full links are in each researcher's report (kept in the conversation only; the links above are the ones to re-open).
