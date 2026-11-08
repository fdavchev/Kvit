import path from 'node:path'
import { findForbiddenLicenses, findWeakCopyleft } from './licensePolicy.mjs'
import { readProductionPackages } from './productionPackages.mjs'

const webRoot = path.resolve(import.meta.dirname, '..')
const packages = readProductionPackages(webRoot)

function describeLicence({ name, version, license }) {
  return `  ${name}@${version}: ${license ?? 'no licence stated'}`
}

const weakCopyleft = findWeakCopyleft(packages)
if (weakCopyleft.length > 0) {
  console.log(`Weak copyleft licences (allowed, listed for review):\n${weakCopyleft.map(describeLicence).join('\n')}`)
}

const forbidden = findForbiddenLicenses(packages)
if (forbidden.length > 0) {
  console.error(
    `${forbidden.length} production packages have a licence the policy forbids (copyleft, unknown or missing):\n` +
      `${forbidden.map(describeLicence).join('\n')}\n` +
      'Remove them, or check the licence by hand and add name@version with the reason to allowOverrides in scripts/licensePolicy.mjs.',
  )
  process.exit(1)
}

console.log(`All ${packages.length} production packages have an allowed licence.`)
