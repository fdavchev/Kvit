import { useIsRestoring } from '@tanstack/react-query'
import { useEffect } from 'react'
import { RouterProvider } from 'react-router/dom'
import { preloadScreens, router } from './router'

export function RouterAfterRestore() {
  const isRestoring = useIsRestoring()

  useEffect(() => {
    if (isRestoring) {
      return
    }
    preloadScreens().catch((error: unknown) => {
      console.error('Loading the other screens in the background failed; each one is loaded again when it is opened', error)
    })
  }, [isRestoring])

  if (isRestoring) {
    return null
  }
  return <RouterProvider router={router} />
}
