import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { closingGroup, closingTexts } from '@/test/closingTestData'
import { anaView, findBarTitled } from '@/test/closingTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { groupNameMaxLength, groupOf, testGroupId } from '@/test/groupTestData'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const oneVeryLongMacedonianWord = 'Прекуокеанскотпатувањезаседумнаесетпријателизаедно'

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

describe('a group name that is one very long word', () => {
  beforeEach(() => {
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('is a name the group form accepts, so it can really happen', () => {
    expect(oneVeryLongMacedonianWord.length).toBeLessThanOrEqual(groupNameMaxLength)
    expect(oneVeryLongMacedonianWord).not.toContain(' ')
  })

  it('may break anywhere in the title of the group screen, so it wraps instead of running off the screen', async () => {
    await renderGroupApp(routes.group(testGroupId), {
      language: 'mk',
      group: groupOf({ name: oneVeryLongMacedonianWord }),
    })

    const title = await screen.findByRole('heading', { level: 1, name: new RegExp(oneVeryLongMacedonianWord) })

    expect(classesOf(title)).toContain('wrap-anywhere')
    expect(classesOf(title)).toContain('min-w-0')
  })

  it('may break anywhere in the closing prompt that names the group', async () => {
    const longNamedClosingGroup = groupOf({ ...closingGroup, name: oneVeryLongMacedonianWord })
    await renderGroupApp(routes.group(testGroupId), anaView(longNamedClosingGroup, 'mk'))

    const bar = await findBarTitled(closingTexts.prompt, 'mk', { name: 'Filip', group: oneVeryLongMacedonianWord })
    const titleId = bar.getAttribute('aria-labelledby')
    const title = titleId === null ? null : document.getElementById(titleId)

    if (title === null) {
      throw new Error('The closing bar has no title element that aria-labelledby points to')
    }
    expect(classesOf(title)).toContain('wrap-anywhere')
    expect(classesOf(title)).toContain('min-w-0')
  })
})
