import { describe, expect, it } from 'vitest'
import components from '../../../components.json'
import { cn } from './cn'

const sourceFiles: string[] = Object.keys(import.meta.glob('/src/**/*.ts'))

describe('cn', () => {
  it('merges class names and lets the later Tailwind class win', () => {
    expect(cn('px-2 text-sm', 'px-4')).toBe('text-sm px-4')
  })

  it('is the file that the shadcn utils alias in components.json points at', () => {
    const alias: string = components.aliases.utils

    expect(alias.startsWith('@/')).toBe(true)
    expect(sourceFiles).toContain(`/src/${alias.slice(2)}.ts`)
  })
})
