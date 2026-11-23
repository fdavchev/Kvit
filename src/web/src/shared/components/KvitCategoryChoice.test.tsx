import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitCategoryChoice, kvitCategoryGridLook } from './KvitCategoryChoice'

const gridClasses = kvitCategoryGridLook.split(/\s+/)

describe('kvitCategoryGridLook', () => {
  it('lays the categories out in four columns on a normal phone', () => {
    expect(gridClasses).toContain('grid-cols-4')
  })

  it('drops to three columns on a screen narrower than 346 px, so a name such as Намирници fits whole', () => {
    expect(gridClasses).toContain('max-[346px]:grid-cols-3')
  })

  it('never uses five columns', () => {
    expect(gridClasses).not.toContain('grid-cols-5')
  })
})

describe('KvitCategoryChoice', () => {
  it('lets the label break at word edges inside the full width of its column', () => {
    render(<KvitCategoryChoice label="Намирници" tile={<span aria-hidden="true">x</span>} onClick={() => {}} />)

    const label = screen.getByText('Намирници')

    expect(Array.from(label.classList)).toEqual(expect.arrayContaining(['w-full', 'text-center', 'break-words']))
    expect(Array.from(label.classList)).not.toContain('truncate')
  })
})
