export type Fields = Record<string, unknown>

export function readObject(value: unknown, what: string): Fields {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    throw new Error(`Expected ${what} as a JSON object, got ${describeValue(value)}`)
  }
  return value as Fields
}

export function readList(fields: Fields, name: string, what: string): unknown[] {
  const value = fields[name]
  if (!Array.isArray(value)) {
    throw new Error(`Expected "${name}" in ${what} to be a list, got ${describeValue(value)}`)
  }
  return value
}

export function readText(fields: Fields, name: string, what: string): string {
  const value = fields[name]
  if (typeof value !== 'string') {
    throw new Error(`Expected "${name}" of ${what} to be a text, got ${describeValue(value)}`)
  }
  return value
}

export function readTextOrNull(fields: Fields, name: string, what: string): string | null {
  const value = fields[name]
  if (value !== null && typeof value !== 'string') {
    throw new Error(
      `Expected "${name}" of ${what} to be a text or null, got ${describeValue(value)}`,
    )
  }
  return value
}

export function readTextList(fields: Fields, name: string, what: string): string[] {
  const list = readList(fields, name, what)
  if (!list.every((item) => typeof item === 'string')) {
    throw new Error(`Expected "${name}" in ${what} to be a list of texts, got ${describeValue(list)}`)
  }
  return list
}

export function readYesNo(fields: Fields, name: string, what: string): boolean {
  const value = fields[name]
  if (typeof value !== 'boolean') {
    throw new Error(
      `Expected "${name}" of ${what} to be true or false, got ${describeValue(value)}`,
    )
  }
  return value
}

export function readCount(fields: Fields, name: string, what: string): number {
  const value = fields[name]
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 0) {
    throw new Error(
      `Expected "${name}" of ${what} to be a whole number of 0 or more, got ${describeValue(value)}`,
    )
  }
  return value
}

export function readCountOrNull(fields: Fields, name: string, what: string): number | null {
  return fields[name] === null ? null : readCount(fields, name, what)
}

export function readPositiveNumber(fields: Fields, name: string, what: string): number {
  const value = fields[name]
  if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0) {
    throw new Error(
      `Expected "${name}" of ${what} to be a number above 0, got ${describeValue(value)}`,
    )
  }
  return value
}

export function readOneOf<Option extends string>(
  fields: Fields,
  name: string,
  options: readonly Option[],
  what: string,
): Option {
  const value = fields[name]
  const option = options.find((allowed) => allowed === value)
  if (option === undefined) {
    throw new Error(
      `Expected "${name}" of ${what} to be one of ${options.join(', ')}, got ${describeValue(value)}`,
    )
  }
  return option
}

export function readDate(fields: Fields, name: string, what: string): string {
  const value = fields[name]
  if (typeof value !== 'string' || Number.isNaN(Date.parse(value))) {
    throw new Error(
      `Expected "${name}" of ${what} to be a date and time, got ${describeValue(value)}`,
    )
  }
  return value
}

export function readDateOrNull(fields: Fields, name: string, what: string): string | null {
  return fields[name] === null ? null : readDate(fields, name, what)
}

function describeValue(value: unknown): string {
  return value === undefined ? 'nothing' : JSON.stringify(value)
}
