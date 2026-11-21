import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { filipMember } from '@/test/memberTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { MemberListItem } from './MemberListItem'

const sixtyLetterName = 'Абвгд'.repeat(12)

async function renderedNameParts() {
  await renderElementWithProviders(
    <ul>
      <MemberListItem
        member={{ ...filipMember, displayName: sixtyLetterName, isOwner: true, isYou: true }}
        showsTakenName={false}
        canClaim={false}
        isClaimPending={false}
        hasOptions
        onClaim={() => {}}
        onOpenOptions={() => {}}
      />
    </ul>,
    '/members',
  )
  const name = screen.getByText(sixtyLetterName)
  const nameLine = name.parentElement
  const textContainer = nameLine?.parentElement
  if (nameLine === null || nameLine === undefined || textContainer === null || textContainer === undefined) {
    throw new Error(`Expected the name to sit in a line inside a text container: ${name.outerHTML}`)
  }
  return { name, nameLine, textContainer }
}

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

describe('a member row with a name of 60 letters and no spaces', () => {
  it('lets the name shrink below its own text width and break inside the word', async () => {
    const { name } = await renderedNameParts()

    expect(sixtyLetterName).toHaveLength(60)
    expect(classesOf(name)).toEqual(expect.arrayContaining(['min-w-0', 'break-words']))
  })

  it('lets the line with the name and the badges wrap, so a badge can drop under the name', async () => {
    const { nameLine } = await renderedNameParts()

    expect(classesOf(nameLine)).toContain('flex-wrap')
  })

  it('lets the text container shrink and grow beside the options button', async () => {
    const { textContainer } = await renderedNameParts()

    expect(classesOf(textContainer)).toEqual(expect.arrayContaining(['min-w-0', 'flex-1']))
  })
})
