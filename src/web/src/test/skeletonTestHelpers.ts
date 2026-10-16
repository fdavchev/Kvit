import { screen } from '@testing-library/react'

export interface LoadingLook {
  skeletonCount: number
  hasSpinner: boolean
}

export function loadingLook(): LoadingLook {
  const status = screen.getByRole('status')
  return {
    skeletonCount: status.querySelectorAll('[data-skeleton]').length,
    hasSpinner: status.querySelector('.animate-spin') !== null,
  }
}
