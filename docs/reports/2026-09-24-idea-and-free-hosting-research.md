# Research: project idea + free hosting (2026-09-24)

Done by the `researcher` subagent (Opus) with web search. All pages checked on 2026-09-24.
Labels: claims with a source link are **VERIFIED by live web check** on that date. Anything marked **NOT VERIFIED** could not be confirmed from a source. Scores and opinions are the researcher's judgement, not facts.

## Short answer

- **Stack (fixed):** C# ASP.NET Core Web API + EF Core + PostgreSQL, React, Docker, GitHub Actions. Everything must stay free, even with long-term real use.
- **Recommended idea:** booking app for **hourly 5-a-side football pitches and padel/tennis courts** ("мал фудбал"). Later, add a **group game layer** where the person who booked invites friends, tracks RSVPs with a waitlist and splits the cost.
- **Recommended hosting:** one **Oracle Cloud Always Free** ARM server running Docker Compose (API + PostgreSQL + Caddy). **DuckDNS** for a free address, with HTTPS from Caddy and Let's Encrypt. React on **Cloudflare Pages**. **UptimeRobot** for monitoring and **GitHub Actions** for building and deploying.
- **Backup hosting:** API on **Render free** (Docker), database on **Neon free**, frontend on Cloudflare Pages.
- **Biggest trap: email.** Without your own domain, Brevo and Resend can't send reliable email. Use **Gmail SMTP** (about 500 a day), a **Telegram bot** or **Web Push** for reminders, or apply for a free **eu.org** domain (approval can take weeks).

### Recent changes to know about
- **Oracle** quietly halved the free ARM allowance to **2 CPU cores / 12 GB RAM** on 2026-06-15. It used to be 4 / 24.
- **UptimeRobot** allows commercial use on the free plan again (help article updated 2026-08-27).
- **Supabase:** paused free projects can now be restored for up to 1 year.
- **Let's Encrypt** certificates get shorter: 64 days from 2027-02-10 and 45 days from 2028-02-16. Caddy renews them automatically.

---

## Part 1: project candidates

Scores are out of 5 for usefulness / hireability / fit with free hosting. They are the researcher's judgement.

### 1. Sports pitch and court booking (recommended), 5 / 5 / 4
Small football pitches and padel courts in Macedonia mostly take bookings by phone, Viber or Instagram. That's a general impression, NOT VERIFIED. The app gives each venue a public page with a live grid of free slots. Players book an hour, the owner confirms or blocks slots, and everyone gets a reminder. Payment is "paid at venue", so no payment API is needed.

**MVP:**
- Venues, courts, opening hours and price rules (peak and off-peak).
- Roles: platform admin, venue owner, venue staff, player.
- A live slot grid that updates over SignalR.
- Booking with a guarantee that the same slot can't be booked twice.
- Cancellation rules.
- Reminders.
- Owner reports: occupancy, no-shows, revenue.

**Hard parts (what impresses employers):**
- **Stopping double bookings under load.** Use a PostgreSQL exclusion constraint on `tstzrange` plus EF Core concurrency tokens. Prove it with a Testcontainers test that fires 50 parallel requests at the same slot.
- **Recurring bookings.** For example "every Tuesday 20:00", with exceptions, time zones and daylight saving changes.
- **Background jobs.** Reminders, releasing unconfirmed holds after N minutes, and no-show tracking. Use Hangfire, Quartz or a hosted service.

**Cost traps:**
- SMS costs money, so use email, Telegram or Web Push.
- A Viber bot costs about €100–115 a month ([Viber](https://help.viber.com/hc/en-us/articles/15247629658525-Bot-commercial-model)), so avoid Viber.
- Online payments carry fees.
- The standard OSM map tile servers aren't meant for heavy use ([OSM](https://operations.osmfoundation.org/policies/tiles/)). A plain address or a "Google Maps" link is enough.

**Other booking niches:**
1. **Football pitches and padel/tennis courts.** Best concurrency demo, and you and your friends are real users.
2. **Barber shops and beauty salons.** Services have different lengths and each staff member has their own calendar, so the scheduling algorithm is harder. Local businesses are everywhere.
3. **Private tutoring.** Weekly recurring slots, prepaid lesson packages that count down, and tutor/student/parent roles.

### 2. Pickup-game and team organizer, 5 / 3 / 5
A recurring weekly game for a group of friends:
- RSVPs with a player cap and a waitlist.
- Automatic promotion from the waitlist when someone drops out.
- Team balancing by rating.
- Splitting the pitch fee.

It's too thin to stand alone for employers, so the researcher suggests adding it as a module inside #1.

### 3. Shared expense splitter and household budget (Splitwise-style), 5 / 4 / 5
- Roommates, couples or trip groups log shared expenses and see simplified "who pays whom" balances. A personal budget mode adds categories and monthly limits.
- **Local extra:** scan the QR code on Macedonian fiscal receipts to fill in the amount, date and shop. Every receipt has a UJP QR code ([source](https://dddinvoices.com/learn/fiscalization-and-real-time-reporting-in-north-macedonia)). The QR format itself is NOT VERIFIED.
- **Hard parts:**
  - The debt-simplification algorithm.
  - MKD/EUR currencies. A free rates source is NOT VERIFIED.
  - Offline sync.
  - Exact money handling with rounding rules and an audit trail.
- **Trap:** automatic bank sync. The usual free option, GoCardless/Nordigen, stopped new signups in July 2025 ([source](https://bankaccountdata.gocardless.com/new-signups-disabled)). Use CSV import or receipt scanning instead.
- **Weaker than booking** on the real-time and concurrency showcase.

### 4. Event ticketing with QR check-in, 4 / 5 / 4
- Organisers create events with limited capacity. Attendees get a signed QR ticket, and staff scan it at the door with an offline-capable PWA.
- **Hard parts:**
  - Stopping overselling, with temporary holds that expire.
  - Offline scanning that syncs later without letting the same ticket in twice.
  - Rate limiting on the sign-up endpoint.

### 5. Volunteer and shift scheduler, 3 / 4 / 5
Managers publish shifts and people sign up or swap them with approval. Hard parts are the swap approval workflow, checking for overlapping shifts, recurring templates and an iCal feed.

**Researcher's recommendation:** build #1 and add #2 once the MVP works. Pick #3 only if daily personal use matters more to you than the concurrency and real-time showcase.

---

## Part 2: free-tier terms (checked 2026-09-24)

| Service | Key terms | Long-term free? |
|---|---|---|
| **Oracle Cloud Always Free** | ARM A1: **2 CPU cores / 12 GB RAM** (cut from 4/24 on 2026-06-15, [InfoQ](https://www.infoq.com/news/2026/07/oracle-cloud-free-tier-limits/)). 200 GB disk in total ([Oracle](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm)). **A card is needed at sign-up** but isn't charged. Free resources **stay after the 30-day trial ends**. You can only create them in your home region. **Idle rule:** a server can be taken back if, over 7 days, CPU, network **and** memory all stay under 20%. "Out of capacity" errors are common; upgrading to Pay As You Go reportedly helps and still costs nothing within the free limits (third-party source). | Yes, with risks. Whether Pay As You Go protects against the idle rule is NOT VERIFIED. Whether Macedonian cards are accepted is NOT VERIFIED. |
| **Cloudflare Pages** | 500 builds a month, 20,000 files, 100 projects, no commercial restriction ([limits, updated 2026-09-05](https://developers.cloudflare.com/pages/platform/limits/)). Not deprecated. | Yes. Best choice for the React frontend. |
| **Vercel Hobby** | **Non-commercial use only** ([docs, updated 2026-09-14](https://vercel.com/docs/plans/hobby)). | Portfolio only. Not allowed if a real venue uses it. |
| **Neon Free** | 0.5 GB, 100 compute-hours a month. Sleeps after 5 minutes idle. No card, no expiry, and going over the limits suspends the database but never deletes data ([Neon](https://neon.com/docs/introduction/plans)). | Yes. Trap: anything that keeps the database awake (Hangfire polling, health checks that hit the database) uses up the hours. |
| **Supabase Free** | 500 MB. **Pauses after about 1 week without activity** and can be restored for up to 1 year ([docs](https://supabase.com/docs/guides/platform/free-project-pausing)). | Second choice after Neon. |
| **Render Free** | Web service sleeps after 15 minutes idle and takes about 1 minute to wake. **The free Postgres is deleted about 30 days after creation**, plus 14 days' grace ([Render](https://render.com/docs/free)). | Web service is OK as a backup. **Never use its free database.** |
| **Azure App Service F1** | Only 60 CPU-minutes a day, no custom domain ([pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)). | Unsuitable. |
| **Brevo Free** | 300 emails a day (third-party sources). Can't properly authenticate a @gmail.com sender, so mail may land in spam ([Brevo](https://help.brevo.com/hc/en-us/articles/14925263522578-Comply-with-Gmail-Yahoo-and-Microsoft-s-requirements-for-email-senders)). | Needs your own domain. |
| **Resend Free** | 3,000 a month, 100 a day, **needs a verified domain** ([pricing](https://resend.com/pricing)). | Needs your own domain. |
| **UptimeRobot Free** | 50 monitors, checks every 5 minutes, commercial use allowed again ([help, updated 2026-08-27](https://help.uptimerobot.com/en/articles/11604710-who-should-use-uptimerobot-s-free-plan)). | Yes. The rules have changed twice, so check again later. |
| **DuckDNS** | Free subdomains. TXT records work, so Let's Encrypt works ([spec](https://www.duckdns.org/spec.jsp)). Donation-funded, with no guarantee it stays free ([FAQ](https://www.duckdns.org/faqs.jsp)). | Yes. Can't be used to authenticate email. |
| **Caddy + Let's Encrypt** | Free, with automatic renewal. Certificate lifetimes get shorter from 2027 ([LE](https://letsencrypt.org/2025/12/02/from-90-to-45)). | Yes. |
| **GitHub Actions** | Unlimited for public repositories. Private repositories get 2,000 minutes a month ([GitHub](https://docs.github.com/en/billing/concepts/product-billing/github-actions)). | Yes. Keep the repository public. |

**Free reminders without a domain:**
- Gmail SMTP with an app password, about 500 recipients a day (third-party source).
- Telegram Bot API, free, about 30 messages a second ([Telegram](https://core.telegram.org/bots/faq)).
- Web Push, free (source NOT VERIFIED).
- A free eu.org domain (manual approval, can take weeks).

**Avoid:**
- Render free Postgres.
- Azure F1.
- Vercel Hobby for real business use.
- Viber bots.
- Bank-sync APIs.
- SMS.

**Backups:** run a nightly `pg_dump` and keep the copy off the server. The exact place to store it is NOT VERIFIED.
