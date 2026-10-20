import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { sheetBodyOf } from '@/test/sheetScrollTestHelpers'
import { KvitForm } from './KvitForm'
import { KvitSheet } from './KvitSheet'
import { kvitGlassPanelLook } from './kvitGlassLooks'
import { useKvitSheet } from './useKvitSheet'

const title = 'Add a name'

function renderSheet(onClose: () => void = () => {}): void {
  render(
    <KvitSheet title={title} onClose={onClose}>
      <p>sheet content</p>
      <button type="button">inner button</button>
    </KvitSheet>,
  )
}

function pressOn(element: HTMLElement): void {
  fireEvent.pointerDown(element)
  fireEvent.mouseDown(element)
  fireEvent.pointerUp(element)
  fireEvent.mouseUp(element)
  fireEvent.click(element)
}

function backdropOf(sheet: HTMLElement): HTMLElement {
  const backdrop = sheet.parentElement
  if (backdrop === null) {
    throw new Error('The sheet has no wrapping backdrop element')
  }
  return backdrop
}

function popupClassTokens(): string[] {
  return screen.getByRole('dialog').className.split(/\s+/)
}

function sheetBody(): HTMLElement {
  return sheetBodyOf(screen.getByRole('dialog'))
}

function hasTokenStartingWith(tokens: string[], prefix: string): boolean {
  return tokens.some((token) => token.startsWith(prefix))
}

async function letPendingTimersRun(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('KvitSheet', () => {
  it('is a dialog named by its title', () => {
    renderSheet()

    expect(screen.getByRole('dialog', { name: title })).toBeTruthy()
  })

  it('shows the title as visible text inside the sheet', () => {
    renderSheet()

    expect(screen.getByRole('dialog').textContent).toContain(title)
  })

  it('shows its children', () => {
    renderSheet()

    expect(screen.getByText('sheet content')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'inner button' })).toBeTruthy()
  })

  it('moves the focus into the sheet', async () => {
    renderSheet()

    await waitFor(() => {
      expect(screen.getByRole('dialog').contains(document.activeElement)).toBe(true)
    })
  })

  describe('the focus after closing', () => {
    function SheetWithFieldOpener({ returnsFocusOnClose }: { returnsFocusOnClose?: () => boolean }) {
      const [isOpen, setIsOpen] = useState<boolean>(false)
      return (
        <>
          <button type="button" onClick={() => setIsOpen(true)}>
            opener
          </button>
          <button type="button">other button</button>
          {isOpen && (
            <KvitSheet title={title} onClose={() => setIsOpen(false)} returnsFocusOnClose={returnsFocusOnClose}>
              <label>
                Name
                <input autoFocus />
              </label>
            </KvitSheet>
          )}
        </>
      )
    }

    async function openFromOpener(): Promise<HTMLElement> {
      const opener = screen.getByRole('button', { name: 'opener' })
      opener.focus()
      fireEvent.click(opener)
      await waitFor(() => {
        expect(document.activeElement).toBe(screen.getByLabelText('Name'))
      })
      return opener
    }

    async function closeWithEscape(): Promise<void> {
      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await letPendingTimersRun()
    }

    it('puts the focus back on the button that opened it, even when a field inside took the focus on open', async () => {
      render(<SheetWithFieldOpener />)
      const opener = await openFromOpener()

      await closeWithEscape()

      expect(document.activeElement).toBe(opener)
    })

    it('leaves the focus where it is when something outside the sheet took it before the sheet closed', async () => {
      function SheetThatMovesFocusOut() {
        const [isOpen, setIsOpen] = useState<boolean>(false)
        const sheet = useKvitSheet()
        return (
          <>
            <button type="button" onClick={() => setIsOpen(true)}>
              opener
            </button>
            <button type="button">other button</button>
            {isOpen && (
              <KvitSheet title={title} onClose={() => setIsOpen(false)} actionsRef={sheet.actionsRef}>
                <label>
                  Name
                  <input autoFocus />
                </label>
                <button
                  type="button"
                  onClick={() => {
                    screen.getByRole('button', { name: 'other button', hidden: true }).focus()
                    sheet.close()
                  }}
                >
                  move away
                </button>
              </KvitSheet>
            )}
          </>
        )
      }
      render(<SheetThatMovesFocusOut />)
      await openFromOpener()

      fireEvent.click(screen.getByRole('button', { name: 'move away' }))
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await letPendingTimersRun()

      expect(document.activeElement).toBe(screen.getByRole('button', { name: 'other button' }))
    })

    it('leaves the focus alone when the sheet says not to return it', async () => {
      render(<SheetWithFieldOpener returnsFocusOnClose={() => false} />)
      await openFromOpener()

      await closeWithEscape()

      expect(document.activeElement).toBe(document.body)
    })
  })

  it('calls onClose when Escape is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onClose).toHaveBeenCalled()
  })

  it('does not call onClose when another key is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Enter' })

    expect(onClose).not.toHaveBeenCalled()
  })

  it('calls onClose when the backdrop around the sheet is pressed', () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(backdropOf(screen.getByRole('dialog')))

    expect(onClose).toHaveBeenCalled()
  })

  it.each([
    ['the title', () => screen.getByText(title)],
    ['the content', () => screen.getByText('sheet content')],
    ['a button inside', () => screen.getByRole('button', { name: 'inner button' })],
  ])('does not call onClose when %s is pressed', (_name, find) => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(find())

    expect(onClose).not.toHaveBeenCalled()
  })

  it('calls onClose exactly once after Escape, once the closing has finished', async () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    await letPendingTimersRun()
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('calls onClose exactly once after the backdrop is pressed, once the closing has finished', async () => {
    const onClose = vi.fn<() => void>()
    renderSheet(onClose)

    pressOn(backdropOf(screen.getByRole('dialog')))

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1)
    })
    await letPendingTimersRun()
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('slides and fades out when it closes', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'data-ending-style:')).toBe(true)
  })

  it('slides and fades in from the starting style when it opens', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'data-starting-style:')).toBe(true)
  })

  it('turns the motion off for people who prefer reduced motion', () => {
    renderSheet()

    expect(hasTokenStartingWith(popupClassTokens(), 'motion-reduce:')).toBe(true)
  })

  it('uses the shared glass panel look', () => {
    renderSheet()

    expect(popupClassTokens()).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(' ')))
  })

  it('keeps the dark backdrop behind the sheet without blurring the page', () => {
    renderSheet()

    const backdropTokens = backdropOf(screen.getByRole('dialog')).previousElementSibling?.className.split(/\s+/) ?? []

    expect(backdropTokens).toContain('bg-black/50')
    expect(hasTokenStartingWith(backdropTokens, 'backdrop-blur')).toBe(false)
  })

  it('never grows taller than the visible window and leaves a gap at the top', () => {
    renderSheet()

    expect(popupClassTokens()).toContain('max-h-[calc(100dvh_-_max(1.5rem,env(safe-area-inset-top)))]')
  })

  it('keeps the title out of the scrolling part, so it stays reachable', () => {
    renderSheet()

    const body = sheetBody()

    expect(body.textContent).toContain('sheet content')
    expect(body.textContent).not.toContain(title)
    expect(screen.getByRole('dialog').firstElementChild?.textContent).toBe(title)
  })

  it('scrolls its content inside the sheet without scrolling the page behind it', () => {
    renderSheet()

    expect(sheetBody().className.split(/\s+/)).toEqual(
      expect.arrayContaining(['min-h-0', 'overflow-y-auto', 'overscroll-contain']),
    )
  })

  it('keeps the bottom safe-area padding inside the scrolling part, so the last item can scroll clear of it', () => {
    renderSheet()

    expect(popupClassTokens()).not.toContain('pb-[max(24px,env(safe-area-inset-bottom))]')
    expect(sheetBody().className.split(/\s+/)).toContain('pb-[max(24px,env(safe-area-inset-bottom))]')
  })

  it('keeps the main button of a form in view while only the fields scroll', () => {
    render(
      <KvitSheet title={title} onClose={() => {}}>
        <KvitForm submitLabel="Save" isPending={false} errorMessage={null} onSubmit={() => {}}>
          <label>
            Name
            <input />
          </label>
        </KvitForm>
      </KvitSheet>,
    )

    const form = sheetBody().querySelector('form')
    const fields = screen.getByLabelText('Name').closest('label')?.parentElement
    const save = screen.getByRole('button', { name: 'Save' })

    expect(form?.parentElement).toBe(sheetBody())
    expect(form?.className.split(/\s+/)).toContain('in-data-kvit-sheet-body:min-h-0')
    expect(fields?.className.split(/\s+/)).toEqual(
      expect.arrayContaining(['in-data-kvit-sheet-body:min-h-0', 'in-data-kvit-sheet-body:overflow-y-auto']),
    )
    expect(fields?.contains(save)).toBe(false)
  })
})
