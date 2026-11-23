import { act, fireEvent, screen } from '@testing-library/react'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { followServerAnswer } from '@/core/api/serverWake'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { fieldLabelled } from '@/test/formTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { routes } from './routes'
import { ServerAsleepBar } from './ServerAsleepBar'

const wakeUrl = 'https://kvit-mk-api.onrender.com/wake'
const refusedText = 'This name is already used.'

let assign: Mock<(url: string) => void>

function asleepAnswer(): Response {
  return new Response('Kvit proxy: the API server is asleep', {
    status: 503,
    headers: { 'content-type': 'text/plain; charset=utf-8', 'x-kvit-server-asleep': '1', 'x-kvit-wake-url': wakeUrl },
  })
}

function NameForm({ errorMessage = null }: { errorMessage?: string | null }) {
  const [name, setName] = useState<string>('')
  return (
    <KvitForm submitLabel="Create" isPending={false} errorMessage={errorMessage} onSubmit={() => {}}>
      <KvitTextField id="group-name" label="Group name" value={name} onChange={setName} />
    </KvitForm>
  )
}

function Screen() {
  const [isFormShown, setIsFormShown] = useState<boolean>(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  return (
    <>
      <ServerAsleepBar />
      <button type="button" onClick={() => setIsFormShown(false)}>
        Remove the form
      </button>
      <button type="button" onClick={() => setErrorMessage(refusedText)}>
        Refuse the save
      </button>
      {isFormShown && <NameForm errorMessage={errorMessage} />}
    </>
  )
}

async function renderScreen() {
  return renderElementWithProviders(<Screen />, routes.newGroup)
}

function typeIntoTheForm(): void {
  fireEvent.input(fieldLabelled('Group name'), { target: { value: 'Greece trip' } })
}

function tapWake(): void {
  fireEvent.click(screen.getByRole('button', { name: translated('en', 'wakingUp.wake') }))
}

function offerWake(): void {
  act(() => followServerAnswer('POST', endpoints.groups, asleepAnswer()))
}

function leavingThePage(): Event {
  const leaving = new Event('beforeunload', { cancelable: true })
  window.dispatchEvent(leaving)
  return leaving
}

beforeEach(() => {
  assign = vi.fn<(url: string) => void>()
  window.sessionStorage.clear()
  vi.stubGlobal('location', { pathname: routes.newGroup, search: '', assign })
})

afterEach(() => {
  act(() => followServerAnswer('GET', endpoints.groups, new Response(null, { status: 204 })))
  vi.unstubAllGlobals()
  window.sessionStorage.clear()
})

describe('the Wake the server button while a form has unsaved typing', () => {
  it('makes the browser ask before the page goes through the wake address', async () => {
    await renderScreen()
    typeIntoTheForm()
    offerWake()

    tapWake()

    expect(assign).toHaveBeenCalledOnce()
    expect(leavingThePage().defaultPrevented).toBe(true)
  })

  it('lets the page go without a question when nothing was typed', async () => {
    await renderScreen()
    offerWake()

    tapWake()

    expect(assign).toHaveBeenCalledOnce()
    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('lets the page go without a question when the typing was sent with a save that is still running', async () => {
    await renderScreen()
    typeIntoTheForm()
    fireEvent.click(screen.getByRole('button', { name: 'Create' }))
    offerWake()

    tapWake()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('lets the page go without a question when the form with the typing is gone', async () => {
    await renderScreen()
    typeIntoTheForm()
    offerWake()
    fireEvent.click(screen.getByRole('button', { name: 'Remove the form' }))

    tapWake()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('makes the browser ask again when the save came back refused and the person did not change anything', async () => {
    await renderScreen()
    typeIntoTheForm()
    fireEvent.click(screen.getByRole('button', { name: 'Create' }))
    fireEvent.click(screen.getByRole('button', { name: 'Refuse the save' }))
    offerWake()

    tapWake()

    expect(leavingThePage().defaultPrevented).toBe(true)
  })

  it('never makes the browser ask when the app wakes the server by itself at opening', async () => {
    await renderScreen()
    typeIntoTheForm()

    act(() => followServerAnswer('GET', endpoints.me, asleepAnswer()))

    expect(assign).toHaveBeenCalledOnce()
    expect(leavingThePage().defaultPrevented).toBe(false)
  })
})
