import { describe, expect, it } from 'vitest'
import {
  allowOverrides,
  findForbiddenLicenses,
  findWeakCopyleft,
} from '../../scripts/licensePolicy.mjs'

interface PackageLicense {
  name: string
  version: string
  license?: string
}

function forbiddenAmong(
  packages: PackageLicense[],
  overrides?: Record<string, string>,
): PackageLicense[] {
  return overrides === undefined
    ? findForbiddenLicenses(packages)
    : findForbiddenLicenses(packages, overrides)
}

function weakAmong(packages: PackageLicense[]): PackageLicense[] {
  return findWeakCopyleft(packages)
}

function sample(license: string | undefined): PackageLicense {
  return { name: 'sample-package', version: '1.0.0', license }
}

const allowedLicenses = [
  'MIT',
  'ISC',
  'BSD-2-Clause',
  'BSD-3-Clause',
  'Apache-2.0',
  '0BSD',
  'CC0-1.0',
  'Unlicense',
  'BlueOak-1.0.0',
  'Python-2.0',
  'CC-BY-4.0',
  '(MIT OR Apache-2.0)',
  'Apache-2.0 AND MIT',
  '(BSD-2-Clause OR MIT OR Apache-2.0)',
  '(MIT OR GPL-3.0-only)',
  '(GPL-2.0-only OR MIT)',
  'MIT OR GPL-3.0-or-later',
  '(MIT AND (GPL-3.0-only OR Apache-2.0))',
  '(MIT OR (GPL-2.0-only AND BSD-3-Clause))',
  'LGPL-2.1',
  'LGPL-2.1-or-later',
  'LGPL-3.0-only',
  'MPL-2.0',
  'mit',
  'apache-2.0',
  'unlicense',
  '(mit or gpl-3.0-only)',
  '  MIT  ',
]

const forbiddenLicenses = [
  'GPL-2.0',
  'GPL-2.0-only',
  'GPL-2.0-or-later',
  'GPL-2.0+',
  'GPL-3.0',
  'GPL-3.0-only',
  'GPL-3.0-or-later',
  'AGPL-1.0-only',
  'AGPL-3.0',
  'AGPL-3.0-only',
  'AGPL-3.0-or-later',
  '(GPL-3.0-only AND MIT)',
  'MIT AND GPL-3.0-only',
  '(GPL-3.0-only OR AGPL-3.0-only)',
  '(GPL-2.0-only OR GPL-3.0-or-later)',
  '(MIT AND (GPL-3.0-only OR AGPL-3.0-only))',
  'gpl-3.0-only',
  'agpl-3.0',
  '(gpl-3.0-only and mit)',
  'UNKNOWN',
  'unknown',
  'UNLICENSED',
  'unlicensed',
  '',
  '   ',
  'SEE LICENSE IN LICENSE',
  'see license in license.md',
  'Custom: https://example.com/license',
  'Custom',
  '(MIT AND UNKNOWN)',
  '(UNKNOWN OR GPL-3.0-only)',
]

const weakCopyleftLicenses = ['LGPL-2.1', 'LGPL-2.1-or-later', 'LGPL-3.0-only', 'MPL-2.0', 'mpl-2.0']

const notWeakLicenses = [
  'MIT',
  'ISC',
  'Apache-2.0',
  'BSD-3-Clause',
  'GPL-3.0-only',
  'AGPL-3.0-or-later',
  'UNKNOWN',
]

describe('findForbiddenLicenses', () => {
  it.each(allowedLicenses)('lets the licence "%s" through', (license) => {
    expect(forbiddenAmong([sample(license)])).toEqual([])
  })

  it.each(forbiddenLicenses)('stops the licence "%s"', (license) => {
    const entry = sample(license)

    expect(forbiddenAmong([entry])).toEqual([entry])
  })

  it('stops a package that has no licence field at all', () => {
    const entry: PackageLicense = { name: 'no-licence-field', version: '2.3.4' }

    expect(forbiddenAmong([entry])).toEqual([entry])
  })

  it('stops a package whose licence is undefined', () => {
    const entry = sample(undefined)

    expect(forbiddenAmong([entry])).toEqual([entry])
  })

  it('returns nothing for an empty list', () => {
    expect(forbiddenAmong([])).toEqual([])
  })

  it('returns only the offenders, as the same entries and in the order they were given', () => {
    const first = { name: 'first', version: '1.0.0', license: 'GPL-3.0-only' }
    const fine = { name: 'fine', version: '1.0.0', license: 'MIT' }
    const second = { name: 'second', version: '2.0.0', license: 'UNKNOWN' }
    const weak = { name: 'weak', version: '3.0.0', license: 'MPL-2.0' }

    const result = forbiddenAmong([first, fine, second, weak])

    expect(result).toHaveLength(2)
    expect(result[0]).toBe(first)
    expect(result[1]).toBe(second)
  })

  it('does not change the list it was given', () => {
    const packages = [sample('GPL-3.0-only'), sample('MIT')]
    const copy = structuredClone(packages)

    forbiddenAmong(packages)

    expect(packages).toEqual(copy)
  })

  it('does not report weak copyleft licences as forbidden', () => {
    const packages = weakCopyleftLicenses.map((license) => sample(license))

    expect(forbiddenAmong(packages)).toEqual([])
  })

  it('stops "SEE LICENSE IN LICENSE" when the override map has no entry for the package', () => {
    const entry = { name: 'sees-a-file', version: '1.0.0', license: 'SEE LICENSE IN LICENSE' }

    expect(forbiddenAmong([entry])).toEqual([entry])
  })
})

describe('findForbiddenLicenses with an allow override', () => {
  const seesAFile = { name: 'sees-a-file', version: '1.0.0', license: 'SEE LICENSE IN LICENSE' }
  const unknownLicence = { name: 'unknown-licence', version: '2.0.0', license: 'UNKNOWN' }
  const scoped = { name: '@scope/odd-licence', version: '3.1.0', license: 'Custom: see site' }

  it('lets a package through when name@version has an override with a reason', () => {
    const overrides = { 'sees-a-file@1.0.0': 'The LICENSE file next to it is MIT, checked by hand' }

    expect(forbiddenAmong([seesAFile], overrides)).toEqual([])
  })

  it('lets a package with an UNKNOWN licence through when it has an override with a reason', () => {
    const overrides = { 'unknown-licence@2.0.0': 'The author confirmed MIT by email' }

    expect(forbiddenAmong([unknownLicence], overrides)).toEqual([])
  })

  it('matches a scoped package name', () => {
    const overrides = { '@scope/odd-licence@3.1.0': 'Reviewed by hand: permissive text' }

    expect(forbiddenAmong([scoped], overrides)).toEqual([])
  })

  it('does not let another version of the same package through', () => {
    const newerVersion = { ...seesAFile, version: '1.0.1' }
    const overrides = { 'sees-a-file@1.0.0': 'The LICENSE file next to it is MIT, checked by hand' }

    expect(forbiddenAmong([newerVersion], overrides)).toEqual([newerVersion])
  })

  it('does not let another package through', () => {
    const overrides = { 'sees-a-file@1.0.0': 'The LICENSE file next to it is MIT, checked by hand' }

    expect(forbiddenAmong([seesAFile, unknownLicence], overrides)).toEqual([unknownLicence])
  })

  it('throws an error that names the package when its override has an empty reason', () => {
    const overrides = { 'sees-a-file@1.0.0': '' }

    expect(() => forbiddenAmong([seesAFile], overrides)).toThrow('sees-a-file@1.0.0')
  })

  it('throws an error that names the package when its override reason is only spaces', () => {
    const overrides = { 'sees-a-file@1.0.0': '   ' }

    expect(() => forbiddenAmong([seesAFile], overrides)).toThrow('sees-a-file@1.0.0')
  })

  it('has a reason for every override that is in the real map, so no package is let through without one', () => {
    const reasons = Object.values(allowOverrides)

    expect(reasons.filter((reason) => typeof reason !== 'string' || reason.trim() === '')).toEqual(
      [],
    )
  })
})

describe('findWeakCopyleft', () => {
  it.each(weakCopyleftLicenses)('reports the licence "%s"', (license) => {
    const entry = sample(license)

    expect(weakAmong([entry])).toEqual([entry])
  })

  it.each(notWeakLicenses)('does not report the licence "%s"', (license) => {
    expect(weakAmong([sample(license)])).toEqual([])
  })

  it('returns nothing for an empty list', () => {
    expect(weakAmong([])).toEqual([])
  })

  it('returns only the weak ones, as the same entries and in the order they were given', () => {
    const lesser = { name: 'lesser', version: '1.0.0', license: 'LGPL-2.1-or-later' }
    const fine = { name: 'fine', version: '1.0.0', license: 'MIT' }
    const mozilla = { name: 'mozilla', version: '2.0.0', license: 'MPL-2.0' }

    const result = weakAmong([lesser, fine, mozilla])

    expect(result).toHaveLength(2)
    expect(result[0]).toBe(lesser)
    expect(result[1]).toBe(mozilla)
  })
})
