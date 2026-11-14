import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

describe('endpoints for the data export', () => {
  it('has the path meExport at /api/me/export', () => {
    expect(endpoints.meExport).toBe('/api/me/export')
  })
})
