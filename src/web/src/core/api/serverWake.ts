import { endpoints } from './endpoints'

export const serverAsleepHeader = 'x-kvit-server-asleep'
export const wakeUrlHeader = 'x-kvit-wake-url'
export const lastWakeVisitKey = 'kvit.lastWakeVisitAt'
export const automaticWakeRepeatGuardMilliseconds = 2 * 60 * 1000
export const retryWhileServerWakesMilliseconds = 3000

const returnToParameter = 'returnTo'
const firstServerErrorStatus = 500
const localHostnames: readonly string[] = ['localhost', '127.0.0.1', '[::1]']

type LastWakeVisit =
  | { kind: 'unreadable' }
  | { kind: 'none' }
  | { kind: 'recent'; waitLeftMilliseconds: number }

let hasMeCheckBeenAnswered = false
let offeredWakeUrl: string | null = null
let wakeUrlOfWait: string | null = null
let waitEndTimer: ReturnType<typeof setTimeout> | null = null
const listeners = new Set<() => void>()

export function followServerAnswer(method: string, path: string, response: Response): void {
  const isMeCheck = method === 'GET' && path === endpoints.me
  const isFirstMeCheckAnswer = isMeCheck && !hasMeCheckBeenAnswered
  if (isMeCheck) {
    hasMeCheckBeenAnswered = true
  }
  if (response.status < firstServerErrorStatus) {
    stopWaitingAfterWakeVisit()
    setOfferedWakeUrl(null)
    return
  }
  const wakeUrl = readWakeUrl(response)
  if (wakeUrl === null) {
    return
  }
  const now = Date.now()
  const lastVisit = readLastWakeVisit(now)
  if (lastVisit.kind === 'recent') {
    waitAfterWakeVisit(wakeUrl, lastVisit.waitLeftMilliseconds)
    return
  }
  if (lastVisit.kind === 'none' && isFirstMeCheckAnswer && rememberWakePageVisit(now)) {
    goToWakePage(wakeUrl)
    return
  }
  setOfferedWakeUrl(wakeUrl)
}

export function wakeServer(wakeUrl: string): void {
  rememberWakePageVisit(Date.now())
  goToWakePage(wakeUrl)
}

function readWakeUrl(response: Response): string | null {
  if (response.headers.get(serverAsleepHeader) === null) {
    return null
  }
  const wakeUrl = response.headers.get(wakeUrlHeader)
  if (wakeUrl === null || !isAllowedWakeUrl(wakeUrl)) {
    console.error(
      `The server is asleep, but its wake address ${JSON.stringify(wakeUrl)} is not an https address (http is only allowed for localhost), so Kvit cannot send you through it`,
    )
    return null
  }
  return wakeUrl
}

function isAllowedWakeUrl(wakeUrl: string): boolean {
  let url: URL
  try {
    url = new URL(wakeUrl)
  } catch {
    return false
  }
  return url.protocol === 'https:' || (url.protocol === 'http:' && localHostnames.includes(url.hostname))
}

function readLastWakeVisit(now: number): LastWakeVisit {
  let savedVisit: string | null
  try {
    savedVisit = window.sessionStorage.getItem(lastWakeVisitKey)
  } catch (error) {
    console.error(
      'Kvit cannot read when it last sent you through the wake page (sessionStorage), so it does not do it again by itself and does not wait for the server quietly',
      error,
    )
    return { kind: 'unreadable' }
  }
  const visitedAt = savedVisit === null ? Number.NaN : Number(savedVisit)
  if (!Number.isFinite(visitedAt) || now - visitedAt >= automaticWakeRepeatGuardMilliseconds) {
    return { kind: 'none' }
  }
  return { kind: 'recent', waitLeftMilliseconds: visitedAt + automaticWakeRepeatGuardMilliseconds - now }
}

function waitAfterWakeVisit(wakeUrl: string, waitLeftMilliseconds: number): void {
  wakeUrlOfWait = wakeUrl
  if (waitEndTimer !== null) {
    return
  }
  waitEndTimer = setTimeout(endWaitAfterWakeVisit, waitLeftMilliseconds)
  notifyListeners()
}

function endWaitAfterWakeVisit(): void {
  const wakeUrl = wakeUrlOfWait
  waitEndTimer = null
  wakeUrlOfWait = null
  notifyListeners()
  setOfferedWakeUrl(wakeUrl)
}

function stopWaitingAfterWakeVisit(): void {
  if (waitEndTimer === null) {
    return
  }
  clearTimeout(waitEndTimer)
  waitEndTimer = null
  wakeUrlOfWait = null
  notifyListeners()
}

function rememberWakePageVisit(now: number): boolean {
  try {
    window.sessionStorage.setItem(lastWakeVisitKey, String(now))
    return true
  } catch (error) {
    console.error(
      'Kvit cannot save when it sent you through the wake page (sessionStorage), so it never sends you there by itself',
      error,
    )
    return false
  }
}

function goToWakePage(wakeUrl: string): void {
  const target = new URL(wakeUrl)
  target.searchParams.set(returnToParameter, window.location.pathname + window.location.search)
  window.location.assign(target.toString())
}

function setOfferedWakeUrl(wakeUrl: string | null): void {
  if (offeredWakeUrl === wakeUrl) {
    return
  }
  offeredWakeUrl = wakeUrl
  notifyListeners()
}

function notifyListeners(): void {
  listeners.forEach((listener) => listener())
}

export function subscribeToServerWake(listener: () => void): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function readOfferedWakeUrl(): string | null {
  return offeredWakeUrl
}

export function readIsWaitingAfterWakeVisit(): boolean {
  return waitEndTimer !== null
}
