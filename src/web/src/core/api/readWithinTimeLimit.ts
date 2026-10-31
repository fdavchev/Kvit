export function readWithinTimeLimit<T>(
  read: Promise<T>,
  whatIsRead: string,
  timeLimitMs: number,
  onTimeout?: () => void,
): Promise<T> {
  let timer: ReturnType<typeof setTimeout> | undefined
  const timeLimit = new Promise<never>((_resolve, reject) => {
    timer = setTimeout(() => {
      onTimeout?.()
      reject(
        new Error(
          `Reading ${whatIsRead} from this browser (IndexedDB) took longer than ${timeLimitMs} ms`,
        ),
      )
    }, timeLimitMs)
  })
  return Promise.race([read, timeLimit]).finally(() => {
    clearTimeout(timer)
  })
}
