import { vi } from 'vitest'

export interface SavedFile {
  fileName: string
  href: string | null
}

export interface FileDownloadStub {
  savedFiles: SavedFile[]
  createdBlobs: Blob[]
  revokedUrls: string[]
  restore: () => void
}

type ObjectUrlFunction = 'createObjectURL' | 'revokeObjectURL'

function replaceUrlFunction(name: ObjectUrlFunction, replacement: (...args: never[]) => unknown): () => void {
  const original = Object.getOwnPropertyDescriptor(URL, name)
  Object.defineProperty(URL, name, { value: replacement, configurable: true, writable: true })
  return () => {
    if (original === undefined) {
      Reflect.deleteProperty(URL, name)
      return
    }
    Object.defineProperty(URL, name, original)
  }
}

export function stubFileDownload(): FileDownloadStub {
  const savedFiles: SavedFile[] = []
  const createdBlobs: Blob[] = []
  const revokedUrls: string[] = []

  const restoreCreate = replaceUrlFunction('createObjectURL', (blob: Blob) => {
    createdBlobs.push(blob)
    return `blob:kvit-test/${createdBlobs.length}`
  })
  const restoreRevoke = replaceUrlFunction('revokeObjectURL', (url: string) => {
    revokedUrls.push(url)
  })
  const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
    savedFiles.push({ fileName: this.download, href: this.getAttribute('href') })
  })

  return {
    savedFiles,
    createdBlobs,
    revokedUrls,
    restore: () => {
      clickSpy.mockRestore()
      restoreCreate()
      restoreRevoke()
    },
  }
}
