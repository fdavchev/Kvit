import { act } from '@testing-library/react'
import { vi, type Mock } from 'vitest'

export type ShareFunction = (data?: ShareData) => Promise<void>

export type WriteTextFunction = (text: string) => Promise<void>

export function stubShare(behaviour: ShareFunction): Mock<ShareFunction> {
  const share = vi.fn<ShareFunction>(behaviour)
  Object.defineProperty(navigator, 'share', { value: share, configurable: true })
  return share
}

export function stubClipboard(behaviour: WriteTextFunction): Mock<WriteTextFunction> {
  const writeText = vi.fn<WriteTextFunction>(behaviour)
  Object.defineProperty(navigator, 'clipboard', {
    value: { writeText },
    configurable: true,
  })
  return writeText
}

export function removeShareAndClipboard(): void {
  Reflect.deleteProperty(navigator, 'share')
  Reflect.deleteProperty(navigator, 'clipboard')
}

export function abortError(): DOMException {
  return new DOMException('The share was closed', 'AbortError')
}

export async function letPendingWorkFinish(): Promise<void> {
  await act(() => new Promise<void>((resolve) => setTimeout(resolve, 0)))
}
