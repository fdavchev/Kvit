import { fireEvent, screen, waitFor } from '@testing-library/react'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import type { GroupSpendingPlan } from '@/core/services/groups/groupSpendingService'
import { testGroup } from '@/test/groupTestData'
import { planOf } from '@/test/groupPlanTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import type { PlanChange } from './GroupPlanSheet'
import { GroupPlan } from './GroupPlan'

vi.mock('./GroupPlanSheet', () => ({
  GroupPlanSheet: ({ onClose }: { onClose: (change: PlanChange) => void }) => (
    <div role="dialog" aria-label="plan sheet">
      <button type="button" onClick={() => onClose('set')}>
        close after setting
      </button>
      <button type="button" onClick={() => onClose('removed')}>
        close after removing
      </button>
      <button type="button" onClick={() => onClose('none')}>
        close without a change
      </button>
    </div>
  ),
}))

const setRowName = translated('en', 'groupPlan.setRow')

function PlanThatArrivesLater({ firstPlan }: { firstPlan: GroupSpendingPlan | null }) {
  const [plan, setPlan] = useState<GroupSpendingPlan | null>(firstPlan)
  return (
    <>
      <button type="button" onClick={() => setPlan(planOf())}>
        plan arrives
      </button>
      <button type="button" onClick={() => setPlan(null)}>
        plan is gone
      </button>
      <GroupPlan group={testGroup} plan={plan} />
    </>
  )
}

async function renderPlan(firstPlan: GroupSpendingPlan | null) {
  await renderElementWithProviders(<PlanThatArrivesLater firstPlan={firstPlan} />, '/plan')
}

function setRow(): HTMLElement {
  return screen.getByRole('button', { name: new RegExp(setRowName) })
}

function planCard(): HTMLElement {
  return screen.getByRole('button', { name: new RegExp(translated('en', 'groupPlan.heading')) })
}

function openSheetFrom(opener: HTMLElement): void {
  opener.focus()
  fireEvent.click(opener)
}

function pressInSheet(name: string): void {
  fireEvent.click(screen.getByRole('button', { name }))
}

describe('GroupPlan focus after the sheet', () => {
  it('moves the focus to the new plan card when the plan arrives after the sheet closed and the row it came from is gone', async () => {
    await renderPlan(null)
    openSheetFrom(setRow())
    pressInSheet('close after setting')

    fireEvent.click(screen.getByRole('button', { name: 'plan arrives' }))

    await waitFor(() => {
      expect(document.activeElement).toBe(planCard())
    })
  })

  it('leaves the focus on the row while the plan has not arrived yet', async () => {
    await renderPlan(null)
    openSheetFrom(setRow())

    pressInSheet('close after setting')

    expect(screen.queryByRole('dialog')).toBeNull()
    expect(document.activeElement).toBe(setRow())
  })

  it('drops the waiting focus request when the sheet is opened again before the plan arrives', async () => {
    await renderPlan(null)
    openSheetFrom(setRow())
    pressInSheet('close after setting')
    openSheetFrom(setRow())

    fireEvent.click(screen.getByRole('button', { name: 'plan arrives' }))

    expect(screen.getByRole('dialog')).toBeTruthy()
    expect(document.activeElement).not.toBe(planCard())
  })

  it('drops the waiting focus request of a removal when the sheet is opened again before the plan is gone', async () => {
    await renderPlan(planOf())
    openSheetFrom(planCard())
    pressInSheet('close after removing')
    openSheetFrom(planCard())

    fireEvent.click(screen.getByRole('button', { name: 'plan is gone' }))

    expect(screen.getByRole('dialog')).toBeTruthy()
    expect(document.activeElement).not.toBe(setRow())
  })

  it('asks for no focus at all when the sheet closes without a change', async () => {
    await renderPlan(null)
    openSheetFrom(setRow())
    pressInSheet('close without a change')

    fireEvent.click(screen.getByRole('button', { name: 'plan arrives' }))

    expect(document.activeElement).not.toBe(planCard())
  })
})
