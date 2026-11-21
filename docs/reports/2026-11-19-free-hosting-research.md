# Can Kvit's server be always awake for free? (research, 2026-11-19)

Asked by Filip after the Render wake trip annoyed him on the phone. Done by the researcher subagent from official pages where possible. Rule checked against: free forever, no bank card, runs our .NET server in Docker. Nothing was signed up for or tried.

## Short answer
No host meets all three (no card, always awake or a few-second wake, runs our Docker image) on an official page. Render stays for now; the wake trip is the price of free hosting.

## The options
| Host | Card? | Sleeps? | Verdict |
|---|---|---|---|
| Render (today) | No | After 15 min; since 2026-11-14 only a person's visit wakes it (VERIFIED by our own live run, not on Render's pages) | Stays for now |
| Railway Free plan | No (VERIFIED) | After 5 to 10 min, but a normal forwarded request wakes it with a short cold start; the first request may get a 502 (VERIFIED) | Best real candidate: no redirect. Catch: $1 of credit a month, enough only because it sleeps most of the time; when it runs out the server stops until next month. Europe region and Docker support NOT VERIFIED |
| Home PC or Raspberry Pi + Cloudflare Tunnel | Tunnel card question NOT VERIFIED | Never | The only truly always-on option. Catches: a permanent address most likely needs a bought domain (about 10 € a year, card); the machine must run day and night; a home power or internet cut takes Kvit down |
| Northflank Sandbox | Its own pages disagree (pricing page: no card; billing docs: a payment method is required) | Never | Unlikely; its docs also say the free tier is not for production |
| Koyeb, Fly.io, Google Cloud Run | Yes (VERIFIED for Koyeb and Fly.io) | | Out |
| Hugging Face Spaces (Docker) | | | Out: Docker Spaces need a paid plan now |
| SnapDeploy, Back4App, Alwaysdata, Zeabur, Choreo | Mixed | | Out: same browser-only wake, too small, US only, or no free plan |

Render has no official way to stay awake on the free plan; only paid instances.

## Recommendation (Claude)
- Stay on Render and make the trip bearable: the app card (installed app opens Kvit's start page) and the calm "Waking up…" after the trip, on `fix/21-small-bugs`.
- If the trip still annoys after that: a separate later phase that tries Railway's Free plan for a week (measure the cold start and how much of the $1 is used). Not on this branch.
- Not recommended: a home server (needs a bought domain and a machine running day and night).

Sources: the researcher's report of 2026-11-19 (official pages: render.com/docs/free, docs.railway.com free-trial and app-sleeping, koyeb.com FAQ, docs.fly.io pricing, northflank.com pricing and billing docs, huggingface.co Spaces docs, developers.cloudflare.com Tunnel docs, dotnet/core supported OS list).
