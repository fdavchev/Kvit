# Research: Google/Apple sign-in + sharing to Viber/WhatsApp (2026-09-24)

Done by the `researcher` subagent with web search on 2026-09-24. Claims with a link were checked by the researcher on the official page on that date (**VERIFIED by live web check**). I didn't open the links myself.

## Sign in with Google: free, no card
- Creating a Google Cloud project and an OAuth client needs **no billing account**. Billing only applies to paid Google APIs ([Google](https://developers.google.com/identity/protocols/oauth2), [Manage OAuth clients](https://support.google.com/cloud/answer/15549257?hl=en)).
- Only ask for the `openid`, `email` and `profile` scopes. They're non-sensitive, so Google's security assessment isn't needed ([scopes](https://developers.google.com/identity/protocols/oauth2/scopes), [verification](https://developers.google.com/identity/protocols/oauth2/production-readiness/sensitive-scope-verification)).
- Set the publishing status to **"In production"**. "Testing" limits you to 100 listed users and makes tokens expire after 7 days ([overview](https://developers.google.com/identity/protocols/oauth2/production-readiness/overview)).
- **Trap:** .NET's built-in `MapIdentityApi` only handles email and password. It doesn't do Google sign-in, so that needs its own endpoint ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-10.0)).
- **Recommended approach: Google Identity Services** (the "Sign in with Google" button or One Tap). The React app gets a signed ID token from Google and posts it to the Kvit API. The API checks the token with `GoogleJsonWebSignature.ValidateAsync` (the audience must be our client ID and the issuer must be Google), then logs the user in. This fits a React app + separate API better than the older redirect flow ([GIS reference](https://developers.google.com/identity/gsi/web/reference/js-reference), [One Tap](https://developers.google.com/identity/gsi/web/guides/display-google-one-tap)).

## Sign in with Apple: not free, dropped
- It needs the Apple Developer Program at **$99 a year**. There's no free option for websites ([Apple](https://developer.apple.com/programs/whats-included/)). That breaks the "free, no card" rule.

## Sharing to Viber/WhatsApp: free, no account or bot
- **Web Share API (`navigator.share`)** is built into browsers and costs nothing. It works in iPhone Safari and Android Chrome, but **not in desktop Chrome**. It needs HTTPS and has to be triggered by a tap ([MDN](https://developer.mozilla.org/en-US/docs/Web/API/Web_Share_API), [caniuse](https://caniuse.com/mdn-api_navigator_share)).
- **WhatsApp link** `https://wa.me/?text=...` is free and needs no business account ([WhatsApp help](https://faq.whatsapp.com/5913398998672934)).
- **Viber link** `viber://forward?text=...` is free and needs no bot. It only works if Viber is installed ([Viber developers](https://developers.viber.com/docs/tools/share-button/)).
- **Plan:** use the phone's share menu first. Where that isn't supported, show WhatsApp and Viber buttons instead.
