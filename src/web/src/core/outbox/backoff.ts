const waitsAfterFailedAttemptsMs: readonly number[] = [0, 2000, 5000, 15000, 30000, 60000]

export function backoffMilliseconds(failedAttempts: number): number {
  const lastIndex = waitsAfterFailedAttemptsMs.length - 1
  return waitsAfterFailedAttemptsMs[Math.min(failedAttempts, lastIndex)]
}
