# Try it yourself: joining a group without an account

You need Docker Desktop open. Start the app with `.\scripts\start-local.ps1` from the project folder. If the database says it is out of date, run this once and start again:
`dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`

You will play two people: **you** (the owner) in a normal window, and **a friend** in a private (incognito) window, so the friend is not signed in as you.

## 1. Make a group (normal window)
1. Sign in as yourself. Create a group, for example "Ohrid trip".
2. Open **Members**, add the names **Marko** and **Grandma**, then copy the invite link.

## 2. Join as a guest (private window)
1. Paste the invite link.
2. You should see **"Who are you? Tap your name:"** with Marko, Grandma and "I'm new".
3. Tap **Marko**. One tap and you are in the group. Nothing else to type.
4. In your normal window, open Members: Marko now has a small **Guest** badge.

## 3. A new person (second private window)
1. Open the same link, tap **I'm new**, type "Ana", tap **Join**.

## 4. What a guest cannot do
In Marko's window: there is no New group button, no One bill, and no "Add a name". Settings shows a **"You're a guest"** card. Tapping **Log out** shows a warning first. Marko can add an expense and delete his own.

## 5. Lost phone
1. In your window, open Members, tap the three dots on Marko, tap **Let in again**.
2. In a new private window, open the link and tap **Marko**. He is back, with his old expenses and balance.

## 6. Make an account
In Ana's window: Settings, **Make an account**, type an email and a password, Save. The Guest badge disappears for you and Ana can log in later on any phone.

## What I could not check
Google sign-in from the Log in sheet, a real phone, and Safari. Please try the Google button once on the real site, and open the link on your phone.
