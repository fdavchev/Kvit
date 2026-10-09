type IntlConstructor =
  | typeof Intl.DateTimeFormat
  | typeof Intl.NumberFormat
  | typeof Intl.RelativeTimeFormat
  | typeof Intl.PluralRules

const macedonianTag = /^mk(-|$)/i
const chromeFallbackLocale = 'en-US'

function isMacedonian(locale: unknown): boolean {
  return macedonianTag.test(String(locale))
}

function listOf(locales: unknown): unknown[] {
  if (locales === undefined) {
    return []
  }
  return Array.isArray(locales) ? locales : [locales]
}

function withoutMacedonian(locales: unknown): string[] {
  return listOf(locales)
    .filter((locale) => !isMacedonian(locale))
    .map((locale) => String(locale))
}

function localesAsChromeResolvesThem(locales: unknown): string | string[] | undefined {
  const asked: unknown[] = listOf(locales)
  if (asked.length === 0) {
    return undefined
  }
  const supported: string[] = withoutMacedonian(locales)
  return supported.length === 0 ? chromeFallbackLocale : supported
}

function chromeWithoutMacedonian<Constructor extends IntlConstructor>(
  original: Constructor,
): Constructor {
  return new Proxy(original, {
    construct: (target: Constructor, args: unknown[]): object => {
      const [locales, ...rest] = args
      return Reflect.construct(target, [localesAsChromeResolvesThem(locales), ...rest])
    },
    get: (target: Constructor, property: string | symbol): unknown => {
      if (property === 'supportedLocalesOf') {
        return (locales: unknown, options: unknown): string[] =>
          Reflect.apply(Reflect.get(target, 'supportedLocalesOf'), target, [
            withoutMacedonian(locales),
            options,
          ])
      }
      return Reflect.get(target, property, target)
    },
  })
}

function replaceInIntl(name: string, replacement: IntlConstructor): void {
  Reflect.set(Intl, name, replacement)
}

export function makeIntlLikeChromeWithoutMacedonian(): void {
  replaceInIntl('DateTimeFormat', chromeWithoutMacedonian(Intl.DateTimeFormat))
  replaceInIntl('NumberFormat', chromeWithoutMacedonian(Intl.NumberFormat))
  replaceInIntl('RelativeTimeFormat', chromeWithoutMacedonian(Intl.RelativeTimeFormat))
}

export function makePluralRulesLikeChromeWithoutMacedonian(): () => void {
  const original: typeof Intl.PluralRules = Intl.PluralRules
  replaceInIntl('PluralRules', chromeWithoutMacedonian(original))
  return () => replaceInIntl('PluralRules', original)
}
