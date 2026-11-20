const failedPictureUrls = new Set<string>()

export function hasPictureFailed(pictureUrl: string): boolean {
  return failedPictureUrls.has(pictureUrl)
}

export function rememberFailedPicture(pictureUrl: string): void {
  failedPictureUrls.add(pictureUrl)
}

export function forgetFailedPictures(): void {
  failedPictureUrls.clear()
}
