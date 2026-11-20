import { useSyncExternalStore } from 'react'

const failedPictureUrls = new Set<string>()
const listeners = new Set<() => void>()

function notifyListeners(): void {
  for (const listener of listeners) {
    listener()
  }
}

export function rememberFailedPicture(pictureUrl: string): void {
  if (failedPictureUrls.has(pictureUrl)) {
    return
  }
  failedPictureUrls.add(pictureUrl)
  notifyListeners()
}

export function forgetFailedPictures(): void {
  if (failedPictureUrls.size === 0) {
    return
  }
  failedPictureUrls.clear()
  notifyListeners()
}

function forgetFailedPicturesWhenThePageIsShown(): void {
  if (document.visibilityState === 'visible') {
    forgetFailedPictures()
  }
}

function subscribe(listener: () => void): () => void {
  if (listeners.size === 0) {
    window.addEventListener('online', forgetFailedPictures)
    document.addEventListener('visibilitychange', forgetFailedPicturesWhenThePageIsShown)
  }
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
    if (listeners.size === 0) {
      window.removeEventListener('online', forgetFailedPictures)
      document.removeEventListener('visibilitychange', forgetFailedPicturesWhenThePageIsShown)
    }
  }
}

export function useHasPictureFailed(pictureUrl: string | null): boolean {
  return useSyncExternalStore(subscribe, () => pictureUrl !== null && failedPictureUrls.has(pictureUrl))
}
