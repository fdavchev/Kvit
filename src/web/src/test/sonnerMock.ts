import { vi } from 'vitest'

export const sonnerMock = {
  toast: Object.assign(vi.fn(), { error: vi.fn(), success: vi.fn() }),
}
