import { describe, expect, it } from 'vitest'
import { isGuid } from './isGuid'

describe('isGuid', () => {
  it.each(['f0000000-0000-4000-8000-000000000001', 'A1B2C3D4-0004-4AAA-8BBB-000000000004'])('accepts %s', (text) => {
    expect(isGuid(text)).toBe(true)
  })

  it.each(['not-a-guid', '', 'f0000000000040008000000000000001', 'f0000000-0000-4000-8000-00000000000', ' f0000000-0000-4000-8000-000000000001'])(
    'refuses "%s"',
    (text) => {
      expect(isGuid(text)).toBe(false)
    },
  )
})
