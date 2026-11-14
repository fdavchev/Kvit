export interface HeaderRule {
  pattern: string
  headers: Map<string, string>
}

export function parseHeadersFile(lines: string[]): HeaderRule[] {
  const rules: HeaderRule[] = []
  for (const line of lines) {
    if (line.trim() === '' || line.trim().startsWith('#')) {
      continue
    }
    if (!/^\s/.test(line)) {
      rules.push({ pattern: line.trim(), headers: new Map() })
      continue
    }
    const currentRule = rules.at(-1)
    const separator = line.indexOf(':')
    if (currentRule === undefined || separator === -1) {
      throw new Error(`The _headers line is not a "Name: value" line under a URL pattern: "${line}"`)
    }
    currentRule.headers.set(
      line.slice(0, separator).trim().toLowerCase(),
      line.slice(separator + 1).trim(),
    )
  }
  return rules
}
