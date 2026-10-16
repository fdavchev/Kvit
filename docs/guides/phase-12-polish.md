# Phase 12: what to do and what to look at

Written for Filip. Plain steps, no commands you don't need.

## 1. Commit and push
Open a terminal in the Kvit folder. You are on the branch `feat/12-polish`. The texts to paste (commit subject, PR title, PR description) are in the chat. After the push, open the pull request on GitHub and wait for the checks (backend and frontend) to turn green. Then you merge it yourself. There is no database change in this phase, so no migration is needed.

## 2. Start the app
1. Open Docker Desktop and wait until it says it is running.
2. In PowerShell, in the Kvit folder, type: `.\scripts\start-local.ps1`
3. Your browser opens the app. When you finish, type: `.\scripts\start-local.ps1 -Stop`

## 3. What to look at on the phone size (or a narrow browser window)
- Tap the tabs on a group (Expenses, Balances, Activity). The selected pill should glide, not jump.
- Open any sheet (for example "Add a name"). It should slide up and look like frosted glass. Text must be easy to read.
- Delete an expense. The "Undo" message should pop in quickly and be easy to read.
- Add or remove a name in Members. Rows should fade in and out.
- Reload the page. While a list loads you should see grey pulsing blocks (Groups, Members, the group tabs). The tester saw a spinner on Home and on the group page itself in the second pass while the first pass saw blocks on Home, so tell me what you see there.
- Record a payment. The big amount should roll its digits.
- Switch to dark mode in Settings. The selected pill should be tan on a dark brown-grey page.
- Switch the language to Macedonian. Money should say «ден.», the category should say «Смештај» and the theme setting should say «Тема».

## 4. What to look at on a wide window (laptop)
- Make the browser window wider than about 1024 pixels. A menu appears on the left, the phone bottom bar disappears.
- Home: big "You are owed" and "You owe" numbers, round buttons, group tiles, recent activity.
- A group: Expenses on the left, balances on the right, a round "Add expense" button at the top.
- Make the window narrow again. It should become exactly the phone app again.
- Tell me if you want the desktop look changed. You said it is good for now.

## 5. Turn off animations (to check the "reduce motion" setting)
In Windows: Settings, Accessibility, Visual effects, turn "Animation effects" off. Reload the app. Pills should jump, the toast should just appear, the skeleton should not pulse, the amount should not roll.

## 6. Tell me
Anything that feels slow, ugly or wrong. I put it into the BACKLOG and you choose what comes first. Release 1 is done when you say the app works for you on something real.
