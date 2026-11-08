import { spawnSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { repositoryRoot, webRoot } from './productionPackages'

interface PackageManifest {
  scripts: Record<string, string>
  dependencies: Record<string, string>
  devDependencies: Record<string, string>
}

const manifest: PackageManifest = JSON.parse(readFileSync(path.join(webRoot, 'package.json'), 'utf8'))
const workflow = readFileSync(path.join(repositoryRoot, '.github', 'workflows', 'ci.yml'), 'utf8')
const licencesPathFromRoot = 'src/web/public/third-party-licenses.txt'
const npmRunReference = /npm run ([\w:-]+)/g
const stepStart = /\n(?= {6}- (?:name|uses):)/

function expandedScript(name: string, alreadyExpanded: string[] = []): string {
  const command = manifest.scripts[name]
  if (command === undefined || alreadyExpanded.includes(name)) {
    return ''
  }
  return command.replace(npmRunReference, (reference, referencedName: string) => {
    const expanded = expandedScript(referencedName, [...alreadyExpanded, name])
    return expanded === '' ? reference : expanded
  })
}

function commandsRunByBuild(): string {
  return [expandedScript('prebuild'), expandedScript('build')].filter((part) => part !== '').join(' && ')
}

function frontendSteps(): string[] {
  const frontendStart = workflow.search(/^ {2}frontend:/m)
  if (frontendStart === -1) {
    throw new Error('ci.yml has no "frontend:" job')
  }
  return workflow.slice(frontendStart).split(stepStart)
}

function indexOfStep(steps: string[], command: string): number {
  return steps.findIndex((step) => new RegExp(`^\\s+run:\\s*${command}\\s*$`, 'm').test(step))
}

describe('package.json scripts', () => {
  it('has a check:licenses script', () => {
    expect(manifest.scripts['check:licenses']).toBeTruthy()
  })

  it('runs scripts/licenses.mjs as part of npm run build', () => {
    expect(commandsRunByBuild()).toContain('scripts/licenses.mjs')
  })

  it('runs scripts/licenses.mjs before the type check and before vite build', () => {
    const commands = commandsRunByBuild()
    const generatorAt = commands.indexOf('scripts/licenses.mjs')

    expect(generatorAt).toBeGreaterThanOrEqual(0)
    expect(generatorAt).toBeLessThan(commands.indexOf('tsc -b'))
    expect(generatorAt).toBeLessThan(commands.indexOf('vite build'))
  })

  it('still runs the type check and vite build in npm run build', () => {
    const commands = commandsRunByBuild()

    expect(commands).toContain('tsc -b')
    expect(commands).toContain('vite build')
  })

  it('keeps the licence tool out of the dependencies that ship, so it can only be a development dependency', () => {
    const licenceTools = Object.keys(manifest.dependencies).filter((name) => /licen[cs]e/i.test(name))

    expect(licenceTools).toEqual([])
  })
})

describe('.github/workflows/ci.yml, frontend job', () => {
  it('runs npm run check:licenses after npm ci', () => {
    const steps = frontendSteps()

    const installAt = indexOfStep(steps, 'npm ci')
    const checkAt = indexOfStep(steps, 'npm run check:licenses')

    expect(installAt).toBeGreaterThanOrEqual(0)
    expect(checkAt).toBeGreaterThan(installAt)
  })

  it('runs the licence check on every push and pull request, with no condition and no permission to fail', () => {
    const steps = frontendSteps()
    const checkStep = steps[indexOfStep(steps, 'npm run check:licenses')]

    expect(checkStep).toBeDefined()
    expect(checkStep).not.toMatch(/^\s+if:/m)
    expect(checkStep).not.toMatch(/continue-on-error/)
  })

  it('still runs lint, build and test', () => {
    const steps = frontendSteps()

    expect(indexOfStep(steps, 'npm run lint')).toBeGreaterThanOrEqual(0)
    expect(indexOfStep(steps, 'npm run build')).toBeGreaterThanOrEqual(0)
    expect(indexOfStep(steps, 'npm test')).toBeGreaterThanOrEqual(0)
  })

  it('does not put the licence check in the backend job', () => {
    const backendJob = workflow.slice(0, workflow.search(/^ {2}frontend:/m))

    expect(backendJob).not.toContain('check:licenses')
  })
})

describe('the generated licence file in git', () => {
  it('is ignored by git, because every build regenerates it', () => {
    const result = spawnSync('git', ['check-ignore', '--no-index', '-q', licencesPathFromRoot], {
      cwd: repositoryRoot,
    })

    expect(result.status).toBe(0)
  })

  it('is not tracked by git', () => {
    const result = spawnSync('git', ['ls-files', '--', licencesPathFromRoot], {
      cwd: repositoryRoot,
      encoding: 'utf8',
    })

    expect(result.stdout.trim()).toBe('')
  })
})
