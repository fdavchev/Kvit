import { Suspense, use, type ComponentType, type ReactElement } from 'react'
import { KvitLoading } from '@/shared/components/KvitLoading'

export class ScreenLoadError extends Error {
  constructor(cause: unknown) {
    super('The code of a screen could not be loaded from the website', { cause })
    this.name = 'ScreenLoadError'
  }
}

export interface LazyScreen {
  (): ReactElement
  preload: () => Promise<void>
}

export function lazyScreen(importScreen: () => Promise<ComponentType>): LazyScreen {
  let loading: Promise<ReactElement> | undefined

  function load(): Promise<ReactElement> {
    if (loading === undefined) {
      const started = importScreen().then(
        (Screen) => <Screen />,
        (error: unknown) => {
          throw new ScreenLoadError(error)
        },
      )
      started.then(
        (screen) => {
          Object.assign(started, { status: 'fulfilled', value: screen })
        },
        (error: unknown) => {
          Object.assign(started, { status: 'rejected', reason: error })
        },
      )
      loading = started
    }
    return loading
  }

  function LoadedScreen() {
    return use(load())
  }

  function Screen() {
    return (
      <Suspense fallback={<KvitLoading />}>
        <LoadedScreen />
      </Suspense>
    )
  }

  Screen.preload = (): Promise<void> =>
    load().then(
      () => undefined,
      (error: unknown) => {
        loading = undefined
        throw error
      },
    )

  return Screen
}
