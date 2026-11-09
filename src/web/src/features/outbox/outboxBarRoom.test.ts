import { describe, expect, it } from 'vitest'
import budgetAddButton from '@/features/budget/overview/components/BudgetAddButton.tsx?raw'
import groupScreen from '@/features/groups/group/components/GroupScreen.tsx?raw'
import homePlusMenu from '@/features/home/components/HomePlusMenu.tsx?raw'

const outboxSpaceVariable = 'var(--outbox-bar-space)'
const termsPillSpaceVariable = 'var(--terms-pill-space)'
const bottomBarHeightVariable = 'var(--bottom-bar-height)'
const arbitraryBottomOffset = /bottom-\[[^\s'"`]+\]/g

function bottomOffsetsIn(source: string): string[] {
  return source.match(arbitraryBottomOffset) ?? []
}

describe('the sticky add buttons make room for the outbox bar', () => {
  it.each([
    ['the plus menu of the home screen', homePlusMenu],
    ['the add button of the Budget screen', budgetAddButton],
  ])('keeps %s above the bottom tab bar and adds the room for the bar to its bottom offset', (_name, source) => {
    const offsets = bottomOffsetsIn(source)

    expect(offsets).toHaveLength(1)
    expect(offsets[0]).toContain(bottomBarHeightVariable)
    expect(offsets[0]).toContain(outboxSpaceVariable)
    expect(offsets[0]).toContain('16px')
  })

  it('adds the room for the bar to both bottom offsets of the floating buttons of the group screen', () => {
    const offsets = bottomOffsetsIn(groupScreen)

    expect(offsets).toHaveLength(2)
    for (const offset of offsets) {
      expect(offset).toContain(outboxSpaceVariable)
    }
  })

  it('keeps the old minimums of the floating buttons of the group screen: 12 px with the closing bar and 24 px without', () => {
    const offsets = bottomOffsetsIn(groupScreen)

    expect(offsets.some((offset) => offset.includes('12px'))).toBe(true)
    expect(offsets.some((offset) => offset.includes('24px'))).toBe(true)
    for (const offset of offsets) {
      expect(offset).toContain('env(safe-area-inset-bottom)')
    }
  })

  it.each([
    ['the plus menu of the home screen', homePlusMenu],
    ['the add button of the Budget screen', budgetAddButton],
    ['the floating buttons of the group screen', groupScreen],
  ])('adds the room for the terms-changed pill to every bottom offset of %s', (_name, source) => {
    const offsets = bottomOffsetsIn(source)

    expect(offsets.length).toBeGreaterThan(0)
    for (const offset of offsets) {
      expect(offset).toContain(termsPillSpaceVariable)
    }
  })
})
