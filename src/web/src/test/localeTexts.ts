export function collectTexts(node: object, prefix = ''): [string, unknown][] {
  return Object.entries(node).flatMap(([key, value]): [string, unknown][] => {
    const path = prefix === '' ? key : `${prefix}.${key}`
    return typeof value === 'object' && value !== null
      ? collectTexts(value, path)
      : [[path, value]]
  })
}
