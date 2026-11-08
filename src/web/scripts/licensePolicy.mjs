const permissiveLicences = new Set([
  'mit',
  'isc',
  'bsd-2-clause',
  'bsd-3-clause',
  'apache-2.0',
  '0bsd',
  'cc0-1.0',
  'unlicense',
  'blueoak-1.0.0',
  'python-2.0',
  'cc-by-4.0',
])

const weakCopyleftLicence = /^(lgpl-(2\.0|2\.1|3\.0)(-only|-or-later|\+)?|mpl-(1\.1|2\.0))$/

const licenceIdentifier = /^[a-z0-9.+-]+$/

export const allowOverrides = Object.freeze({})

function parseAlternatives(license) {
  const tokens = license.toLowerCase().match(/\(|\)|[^\s()]+/g) ?? []
  let position = 0

  function parseAtom() {
    const token = tokens[position]
    if (token === '(') {
      position += 1
      const inner = parseOr()
      if (inner === undefined || tokens[position] !== ')') {
        return undefined
      }
      position += 1
      return inner
    }
    if (token === undefined || token === 'and' || token === 'or' || !licenceIdentifier.test(token)) {
      return undefined
    }
    position += 1
    return [[token]]
  }

  function parseAnd() {
    let combinations = [[]]
    for (;;) {
      const atom = parseAtom()
      if (atom === undefined) {
        return undefined
      }
      combinations = combinations.flatMap((combination) =>
        atom.map((alternative) => [...combination, ...alternative]),
      )
      if (tokens[position] !== 'and') {
        return combinations
      }
      position += 1
    }
  }

  function parseOr() {
    const alternatives = []
    for (;;) {
      const conjunction = parseAnd()
      if (conjunction === undefined) {
        return undefined
      }
      alternatives.push(...conjunction)
      if (tokens[position] !== 'or') {
        return alternatives
      }
      position += 1
    }
  }

  const alternatives = parseOr()
  return alternatives === undefined || position !== tokens.length ? [] : alternatives
}

function canBeUsedUnder(license, isAccepted) {
  if (typeof license !== 'string') {
    return false
  }
  return parseAlternatives(license).some((alternative) => alternative.every(isAccepted))
}

function isPermissive(identifier) {
  return permissiveLicences.has(identifier)
}

function isPermissiveOrWeakCopyleft(identifier) {
  return isPermissive(identifier) || weakCopyleftLicence.test(identifier)
}

function checkOverrideReasons(overrides) {
  for (const [packageKey, reason] of Object.entries(overrides)) {
    if (typeof reason !== 'string' || reason.trim() === '') {
      throw new Error(
        `The licence override for ${packageKey} has no reason. Write why this package may be used, or remove the override.`,
      )
    }
  }
}

export function findForbiddenLicenses(packages, overrides = allowOverrides) {
  checkOverrideReasons(overrides)
  return packages.filter(
    ({ name, version, license }) =>
      !Object.hasOwn(overrides, `${name}@${version}`) &&
      !canBeUsedUnder(license, isPermissiveOrWeakCopyleft),
  )
}

export function findWeakCopyleft(packages) {
  return packages.filter(
    ({ license }) =>
      !canBeUsedUnder(license, isPermissive) && canBeUsedUnder(license, isPermissiveOrWeakCopyleft),
  )
}
