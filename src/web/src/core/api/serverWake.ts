import { endpoints } from './endpoints'

export const serverAsleepHeader = 'x-kvit-server-asleep'
export const wakeUrlHeader = 'x-kvit-wake-url'
export const lastWakeVisitKey = 'kvit.lastWakeVisitAt'
export const automaticWakeRepeatGuardMilliseconds = 2 * 60 * 1000

const returnToParameter = 'returnTo'
const firstServerErrorStatus = 500
const localHostnames: readonly string[] = ['localhost', '127.0.0.1', '[::1]']

let hasMeCheckBeenAnswered = false
let offeredWakeUrl: string | null = null
const listeners = new Set<() => void>()

export function followServerAnswer(method: string, path: string, response: Response): void {
  const isMeCheck = method === 'GET' && path === endpoints.me
  const isFirstMeCheckAnswer = isMeCheck && !hasMeCheckBeenAnswered
  if (isMeCheck) {
    hasMeCheckBeenAnswered = true
  }
  if (response.status < firstServerErrorStatus) {
    setOfferedWakeUrl(null)
    return
  }
  const wakeUrl = readWakeUrl(response)
  if (wakeUrl === null) {
    return
  }
  if (isFirstMeCheckAnswer && wakeAutomatically(wakeUrl)) {
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

function wakeAutomatically(wakeUrl: string): boolean {
  const now = Date.now()
  if (wasWakePageVisitedRecently(now) || !rememberWakePageVisit(now)) {
    return false
  }
  goToWakePage(wakeUrl)
  return true
}

function wasWakePageVisitedRecently(now: number): boolean {
  let savedVisit: string | null
  try {
    savedVisit = window.sessionStorage.getItem(lastWakeVisitKey)
  } catch (error) {
    console.error(
      'Kvit cannot read when it last sent you through the wake page (sessionStorage), so it does not do it again by itself',
      error,
    )
    return true
  }
  const visitedAt = savedVisit === null ? Number.NaN : Number(savedVisit)
  return Number.isFinite(visitedAt) && now - visitedAt < automaticWakeRepeatGuardMilliseconds
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
  listeners.forEach((listener) => listener())
}

export function subscribeToWakeOffer(listener: () => void): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function readOfferedWakeUrl(): string | null {
  return offeredWakeUrl
}
