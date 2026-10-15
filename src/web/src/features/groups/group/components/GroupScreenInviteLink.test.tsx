import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { finishedGroupWithoutAnswers } from '@/test/closingTestData'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const openOneBill = groupOf({ kind: 'OneBill', memberCount: 3 })
const finishedOneBill = groupOf({ ...finishedGroupWithoutAnswers, kind: 'OneBill', memberCount: 3 })

async function renderGroupScreen(group: Group, language: Language = 'en'): Promise<void> {
  await renderGroupApp(routes.group(testGroupId), { group, language, expenses: [] })
  await screen.findByRole('heading', { level: 1, name: new RegExp(group.name) })
}

function shareButtons(language: Language = 'en'): HTMLElement[] {
  return screen.queryAllByRole('button', { name: translated(language, 'group.shareLink') })
}

describe('Share invite link on the group screen', () => {
  beforeEach(() => {
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('is one button next to Members and Settings on an open One bill', async () => {
    await renderGroupScreen(openOneBill)

    expect(shareButtons()).toHaveLength(1)
  })

  it('is gone on a finished One bill, because nobody can join a finished group', async () => {
    await renderGroupScreen(finishedOneBill)

    expect(shareButtons()).toHaveLength(0)
  })

  it.each(languages)('is gone on a finished One bill, also in %s', async (language) => {
    await renderGroupScreen(finishedOneBill, language)

    expect(shareButtons(language)).toHaveLength(0)
  })
})
