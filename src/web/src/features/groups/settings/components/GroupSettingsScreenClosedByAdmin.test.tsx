import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import {
  asMemberOf,
  closedByAdminFinishedGroup,
  closedByAdminGroup,
  closingTexts,
} from '@/test/closingTestData'
import { groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  noContentAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupSettingsScreen } from './GroupSettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const leavePath = `${groupPath}/leave`

const openOwnerGroup = groupOf({ memberCount: 5 })
const openMemberGroup = groupOf({ isOwner: false, memberCount: 5 })

async function renderSettings(group: Group, answers: Record<string, AnswerFactory> = {}) {
  const fetchMock = stubFetchByRequest({ [`GET ${groupPath}`]: jsonAnswer(group), ...answers })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupSettings(group.id),
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('heading', { level: 1, name: translated('en', 'groupSettings.title') })
  return { fetchMock, ...rendered }
}

function saveButton(): HTMLElement | null {
  return screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })
}

function deleteButton(): HTMLElement | null {
  return screen.queryByRole('button', { name: translated('en', 'groupSettings.delete') })
}

function unlockButton(): HTMLElement | null {
  return screen.queryByRole('button', { name: closingTexts.unlock.en })
}

function leaveButton(): HTMLButtonElement | null {
  return screen.queryByRole<HTMLButtonElement>('button', { name: translated('en', 'groupSettings.leave') })
}

function countSwitch(): HTMLElement | null {
  return screen.queryByRole('switch', { name: translated('en', 'groupSettings.countInBudget') })
}

describe('the group settings when the owner of Kvit closed the group', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('for the owner of the group', () => {
    it('has Save, Delete group and the budget switch and no Unlock while the group is open, so the next tests prove something', async () => {
      await renderSettings(openOwnerGroup)

      expect(saveButton()).not.toBeNull()
      expect(deleteButton()).not.toBeNull()
      expect(countSwitch()).not.toBeNull()
      expect(unlockButton()).toBeNull()
    })

    it.each([
      ['an open group', closedByAdminGroup],
      ['a finished group', closedByAdminFinishedGroup],
    ])('has no Unlock for changes for %s that the owner of Kvit closed, because the owner of the group cannot unlock it', async (_name, group) => {
      await renderSettings(group)

      expect(unlockButton()).toBeNull()
    })

    it('has no Save, no Delete group and no budget switch, because every change is refused', async () => {
      await renderSettings(closedByAdminGroup)

      expect(saveButton()).toBeNull()
      expect(deleteButton()).toBeNull()
      expect(countSwitch()).toBeNull()
    })

    it('keeps the Report this group link', async () => {
      await renderSettings(closedByAdminGroup)

      expect(screen.getByText(translated('en', 'groupSettings.report'))).toBeTruthy()
    })
  })

  describe('for a member who is not the owner', () => {
    it('has no Save, but Leave group and the budget switch while the group is open, so the next tests prove something', async () => {
      await renderSettings(openMemberGroup)

      expect(saveButton()).toBeNull()
      expect(leaveButton()).not.toBeNull()
      expect(countSwitch()).not.toBeNull()
    })

    it('has neither Save nor Unlock for changes nor the budget switch, but keeps Leave group enabled', async () => {
      await renderSettings(asMemberOf(closedByAdminGroup))

      expect(saveButton()).toBeNull()
      expect(unlockButton()).toBeNull()
      expect(deleteButton()).toBeNull()
      expect(countSwitch()).toBeNull()
      expect(leaveButton()?.disabled).toBe(false)
    })

    it('does not say that nothing can be changed, because the member can still leave', async () => {
      await renderSettings(asMemberOf(closedByAdminGroup))

      expect(screen.queryByText(closingTexts.settingsFrozen.en)).toBeNull()
    })

    it('leaves the group with the Leave group button', async () => {
      const { fetchMock, router } = await renderSettings(asMemberOf(closedByAdminGroup), {
        [`POST ${leavePath}`]: noContentAnswer(),
      })

      fireEvent.click(leaveButton() as HTMLElement)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(requestCount(fetchMock, 'POST', leavePath)).toBe(1)
    })
  })
})
