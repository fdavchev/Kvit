import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { makeIntlLikeChromeWithoutMacedonian } from './chromeWithoutMacedonian'

makeIntlLikeChromeWithoutMacedonian()

afterEach(() => {
  cleanup()
  vi.unstubAllEnvs()
})
