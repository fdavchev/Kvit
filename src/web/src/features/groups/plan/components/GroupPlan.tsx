import type { TFunction } from 'i18next'
import { Fragment, useId, useState, type ReactNode, type Ref } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { GroupSpendingPlan } from '@/core/services/groups/groupSpendingService'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { KvitProgressBar } from '@/shared/components/KvitProgressBar'
import { useFocusWhenOpenerIsGone, type FocusAfterSheet } from '@/shared/hooks/useFocusWhenOpenerIsGone'
import { cn } from '@/shared/utils/cn'
import { currencyLabel, formatMoney, moneyParts, type Currency } from '@/shared/utils/formatMoney'
import { progressStateOf, type ProgressState } from '@/shared/utils/progressState'
import { balancesCardLook } from '../../balances/balancesLooks'
import { canChangeGroup } from '../../shared/groupAccess'
import { GroupPlanSheet, type PlanChange } from './GroupPlanSheet'

const targetEmoji = '\u{1F3AF}'
const slotMark = '\u0000'
const otherCurrency: Record<Currency, Currency> = { MKD: 'EUR', EUR: 'MKD' }

const stateLineLooks: Record<ProgressState, string> = {
  ok: 'text-muted-foreground',
  near: 'text-warning',
  over: 'text-negative',
}

interface PlanFocusAfterSheet extends FocusAfterSheet {
  hasPlan: boolean
}

interface GroupPlanProps {
  group: Group
  plan: GroupSpendingPlan | null
}

export function GroupPlan({ group, plan }: GroupPlanProps) {
  const [isSheetOpen, setIsSheetOpen] = useState<boolean>(false)
  const [sheetOpener, setSheetOpener] = useState<Element | null>(null)
  const [focusAfterSheet, setFocusAfterSheet] = useState<PlanFocusAfterSheet | null>(null)
  const canSetPlan: boolean = group.isOwner && canChangeGroup(group)
  const showsChangedPlan: boolean = focusAfterSheet !== null && focusAfterSheet.hasPlan === (plan !== null)
  const controlRef = useFocusWhenOpenerIsGone<HTMLButtonElement>(focusAfterSheet, canSetPlan && showsChangedPlan, () =>
    setFocusAfterSheet(null),
  )

  function openSheet(): void {
    setSheetOpener(document.activeElement)
    setIsSheetOpen(true)
  }

  function closeSheet(change: PlanChange): void {
    setIsSheetOpen(false)
    if (change !== 'none') {
      setFocusAfterSheet({ opener: sheetOpener, hasPlan: change === 'set' })
    }
  }

  function renderPlanPart(): ReactNode {
    if (plan !== null) {
      return (
        <PlanCard plan={plan} controlRef={controlRef} onOpen={canSetPlan ? openSheet : null} />
      )
    }
    return canSetPlan && <SetPlanRow controlRef={controlRef} onOpen={openSheet} />
  }

  return (
    <>
      {renderPlanPart()}
      {isSheetOpen && <GroupPlanSheet group={group} plan={plan} opener={sheetOpener} onClose={closeSheet} />}
    </>
  )
}

interface SetPlanRowProps {
  controlRef: Ref<HTMLButtonElement>
  onOpen: () => void
}

function SetPlanRow({ controlRef, onOpen }: SetPlanRowProps) {
  const { t } = useTranslation()

  return (
    <button
      ref={controlRef}
      type="button"
      className="pressable mb-3.5 flex min-h-11 w-full items-center gap-3 rounded-[20px] bg-card px-4 py-2.5 text-left text-card-foreground hover:bg-secondary-hover"
      onClick={onOpen}
    >
      <span aria-hidden="true" className="text-[1.375rem] leading-none">
        {targetEmoji}
      </span>
      <span className="min-w-0 flex-1 font-semibold break-words">{t('groupPlan.setRow')}</span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </button>
  )
}

interface PlanCardProps {
  plan: GroupSpendingPlan
  controlRef: Ref<HTMLButtonElement>
  onOpen: (() => void) | null
}

function PlanCard({ plan, controlRef, onOpen }: PlanCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const headingId = useId()
  const state = progressStateOf(plan.spentMinor, plan.amountMinor)
  const content = (
    <span className="flex w-full flex-col gap-1.5">
      <span id={headingId} role="heading" aria-level={2} className="text-[0.8125rem] font-bold text-muted-foreground">
        {t('groupPlan.heading')}
      </span>
      <span className="flex w-full flex-wrap items-baseline gap-x-3 gap-y-0.5">
        <span className="min-w-0 flex-1 basis-40 font-bold">
          <SpentOfAmountLine plan={plan} language={language} />
        </span>
        <span className="text-[0.8125rem] text-muted-foreground">
          {t('groupPlan.each', {
            amount: formatMoney(plan.perPersonMinor, plan.currency, language),
            count: plan.memberCount,
          })}
        </span>
      </span>
      <KvitProgressBar label={t('groupPlan.heading')} spentMinor={plan.spentMinor} limitMinor={plan.amountMinor} />
      <span className={cn('text-[0.8125rem] font-semibold', stateLineLooks[state])}>
        {stateLineOf(state, plan, language, t)}
      </span>
      {plan.includedOtherCurrencyMinor > 0 && (
        <span className="text-[0.8125rem] text-muted-foreground">
          {t('groupPlan.included', {
            amount: formatMoney(plan.includedOtherCurrencyMinor, otherCurrency[plan.currency], language),
            currency: currencyLabel(plan.currency, language),
          })}
        </span>
      )}
    </span>
  )

  if (onOpen === null) {
    return (
      <section aria-labelledby={headingId} className={cn(balancesCardLook, 'mb-3.5')}>
        {content}
      </section>
    )
  }
  return (
    <section aria-labelledby={headingId} className="mb-3.5 rounded-[20px] bg-card text-card-foreground">
      <button
        ref={controlRef}
        type="button"
        className="pressable flex min-h-11 w-full rounded-[20px] px-4 py-3.5 text-left hover:bg-secondary-hover"
        onClick={onOpen}
      >
        {content}
      </button>
    </section>
  )
}

interface SpentOfAmountLineProps {
  plan: GroupSpendingPlan
  language: Language
}

function SpentOfAmountLine({ plan, language }: SpentOfAmountLineProps) {
  const { t } = useTranslation()
  const amounts: Record<string, ReactNode> = {
    spent: <KvitAmount parts={moneyParts(plan.spentMinor, plan.currency, language)} />,
    amount: <KvitAmount parts={moneyParts(plan.amountMinor, plan.currency, language)} />,
  }
  const pieces = t('groupPlan.ofAmount', {
    spent: `${slotMark}spent${slotMark}`,
    amount: `${slotMark}amount${slotMark}`,
  }).split(slotMark)

  return (
    <span>
      {pieces.map((piece, index) => {
        if (index % 2 === 0) {
          return piece
        }
        const amount = amounts[piece]
        if (amount === undefined) {
          throw new Error(`The text groupPlan.ofAmount has the unknown placeholder "${piece}"`)
        }
        return <Fragment key={piece}>{amount}</Fragment>
      })}
    </span>
  )
}

function stateLineOf(
  state: ProgressState,
  plan: GroupSpendingPlan,
  language: Language,
  t: TFunction,
): string {
  if (state === 'over') {
    return t('groupPlan.over', { amount: formatMoney(plan.overMinor, plan.currency, language) })
  }
  if (state === 'near') {
    return t('groupPlan.near', { amount: formatMoney(plan.amountMinor - plan.spentMinor, plan.currency, language) })
  }
  return t('groupPlan.onTrack')
}
