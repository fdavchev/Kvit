export function readRouterStateText(state: unknown, name: string): string | null {
  if (typeof state !== 'object' || state === null) {
    return null
  }
  const value: unknown = Reflect.get(state, name)
  return typeof value === 'string' ? value : null
}
