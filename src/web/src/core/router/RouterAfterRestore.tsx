import { useIsRestoring } from '@tanstack/react-query'
import { RouterProvider } from 'react-router/dom'
import { router } from './router'

export function RouterAfterRestore() {
  const isRestoring = useIsRestoring()
  if (isRestoring) {
    return null
  }
  return <RouterProvider router={router} />
}
