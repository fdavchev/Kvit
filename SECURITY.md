# Security

Kvit is a free project for family and friends, run by one person. There is no bug bounty, but real reports are welcome and will be read.

## How to report a problem
- **Email:** kvit.support@gmail.com. Put "Security" in the subject.
- **Or on GitHub:** use "Report a vulnerability" on the Security tab of this repository (private vulnerability reporting), if it shows for you.

Please include what you found, how to see it, and what you think it could do. Do not post it in a public issue before it is fixed. Do not read, change or delete other people's data to prove a point, and do not run attacks that slow the site down for everyone.

## What to expect
- A first answer within about a week (this is a one-person project, so it can take longer).
- A fix, or a reason why it is not a problem, and a thank-you if you want one.

## What Kvit tries to protect
- Passwords are stored only as scrambled hashes. The login cookie is HttpOnly, Secure and SameSite Lax. The server answers only through the Cloudflare proxy.
- Secrets (database string, proxy secret, Google client secret, backup passphrase) are kept only in the Render, Cloudflare and GitHub secret settings, never in the repository.
- The data model keeps each person's personal budget private; groups show their data only to their members.

## Supported versions
Only the live site (`https://kvit-mk.pages.dev`) and the `main` branch.

The machine-readable version of this page is `/.well-known/security.txt` on the site. Its `Expires` date must be renewed every year (a reminder is in `docs/BACKLOG.md`).
