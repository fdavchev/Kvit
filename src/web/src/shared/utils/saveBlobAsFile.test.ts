import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { stubFileDownload, type FileDownloadStub } from '@/test/fileDownloadTestHelpers'
import { saveBlobAsFile } from './saveBlobAsFile'

describe('saveBlobAsFile', () => {
  let download: FileDownloadStub

  beforeEach(() => {
    download = stubFileDownload()
  })

  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve))
    download.restore()
  })

  it('clicks one link whose download name is the given file name', () => {
    const blob = new Blob(['{}'], { type: 'application/json' })

    saveBlobAsFile(blob, 'kvit-my-data-2026-11-04.json')

    expect(download.savedFiles).toHaveLength(1)
    expect(download.savedFiles[0].fileName).toBe('kvit-my-data-2026-11-04.json')
  })

  it('points the link at an object address made from the given blob', () => {
    const blob = new Blob(['{}'], { type: 'application/json' })

    saveBlobAsFile(blob, 'kvit-my-data.json')

    expect(download.createdBlobs).toEqual([blob])
    expect(download.savedFiles[0].href).toBe('blob:kvit-test/1')
  })

  it('gives the object address back to the browser once the file is saved', async () => {
    saveBlobAsFile(new Blob(['{}']), 'kvit-my-data.json')

    await expect.poll(() => download.revokedUrls).toEqual(['blob:kvit-test/1'])
  })

  it('leaves no link behind in the page', () => {
    saveBlobAsFile(new Blob(['{}']), 'kvit-my-data.json')

    expect(document.querySelector('a[download]')).toBeNull()
  })

  it('saves two files with two different object addresses', () => {
    saveBlobAsFile(new Blob(['1']), 'first.json')
    saveBlobAsFile(new Blob(['2']), 'second.json')

    expect(download.savedFiles.map((file) => file.href)).toEqual(['blob:kvit-test/1', 'blob:kvit-test/2'])
    expect(download.savedFiles.map((file) => file.fileName)).toEqual(['first.json', 'second.json'])
  })
})
