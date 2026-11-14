import { readFileSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { readProductionPackages } from './productionPackages.mjs'

const webRoot = path.resolve(import.meta.dirname, '..')
const outputFile = path.join(webRoot, 'public', 'third-party-licenses.txt')
const divider = '='.repeat(80)

function readLicenceText(file) {
  return readFileSync(file, 'utf8').replace(/^﻿/, '').replace(/\r\n?/g, '\n').trimEnd()
}

function describePackage(installed) {
  const lines = [
    divider,
    `${installed.name} ${installed.version}`,
    `Licence: ${installed.license ?? 'not stated by the package'}`,
  ]
  if (installed.repository !== undefined) {
    lines.push(`Repository: ${installed.repository}`)
  }
  if (installed.homepage !== undefined) {
    lines.push(`Homepage: ${installed.homepage}`)
  }
  lines.push('')
  if (installed.licenceFiles.length === 0) {
    lines.push('The package ships no licence file.', '')
  }
  for (const file of installed.licenceFiles) {
    lines.push(`--- ${path.basename(file)} ---`, readLicenceText(file), '')
  }
  return lines.join('\n')
}

const packages = readProductionPackages(webRoot)
const introduction = [
  'Third-party software in Kvit',
  '',
  `Kvit is built with the ${packages.length} open-source packages below.`,
  'Each one keeps its own licence, given in full where the package ships its licence file.',
  '',
].join('\n')

writeFileSync(outputFile, [introduction, ...packages.map(describePackage)].join('\n'), 'utf8')
console.log(`Wrote the licences of ${packages.length} packages to ${path.relative(webRoot, outputFile)}`)
