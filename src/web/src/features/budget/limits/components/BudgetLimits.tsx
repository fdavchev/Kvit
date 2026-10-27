import { useEffect, useId, useRef, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { LimitRow, MonthLimits } from '@/core/services/personalEntries/personalEntriesService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { cn } from '@/shared/utils/cn'
import { formatMoney, moneyParts } from '@/shared/utils/formatMoney'
import { usePersonalCategoriesWithArchived } from '../../categories/hooks/usePersonalCategoriesWithArchived'
import { personalCategoryName } from '../../categories/personalCategoryName'
import {
  hasAnyLimit,
  limitFillRatio,
  limitMinorOf,
  limitStateOf,
  wholeMonthEmoji,
  type LimitState,
  type LimitTarget,
} from '../budgetLimits'

const targetEmoji = '\u{1F3AF}'

const fillLooks: Record<LimitState, string> = {
  ok: 'bg-positive',
  near: 'bg-warning',
  over: 'bg-negative',
}

export interface LimitFocusRequest {
  target: LimitTarget
  opener: Element | null
}

type LimitFocus = ({ part: 'row'; categoryId: string | null } | { part: 'setLimit' }) & { opener: Element | null }

interface LimitsPartProps {
  limits: MonthLimits
  focus: LimitFocus | null
  onOpenLimit: (target: LimitTarget) => void
  onSetLimit: () => void
  onFocusHandled: () => void
}

interface BudgetLimitsProps extends Omit<LimitsPartProps, 'focus'> {
  focusRequest: LimitFocusRequest | null
}

export function BudgetLimits({ focusRequest, ...props }: BudgetLimitsProps) {
  const part: LimitsPartProps = { ...props, focus: focusOf(props.limits, focusRequest) }

  if (!hasAnyLimit(props.limits)) {
    return <FirstLimitRow {...part} />
  }
  if (props.limits.categories.length === 0) {
    return <LimitsCard {...part} categories={[]} />
  }
  return <LimitsCardWithCategoryNames {...part} />
}

function focusOf(limits: MonthLimits, request: LimitFocusRequest | null): LimitFocus | null {
  if (request === null) {
    return null
  }
  const categoryId = request.target.category?.id ?? null
  if (limitMinorOf(limits, categoryId) === null) {
    return { part: 'setLimit', opener: request.opener }
  }
  return { part: 'row', categoryId, opener: request.opener }
}

function useFocusWhenOpenerIsGone<T extends HTMLElement>(
  focus: LimitFocus | null,
  isTarget: boolean,
  onFocusHandled: () => void,
): RefObject<T | null> {
  const ref = useRef<T>(null)

  useEffect(() => {
    if (focus === null || !isTarget) {
      return
    }
    if (focus.opener?.isConnected !== true) {
      ref.current?.focus()
    }
    onFocusHandled()
  }, [focus, isTarget, onFocusHandled])
  return ref
}

function FirstLimitRow({ focus, onSetLimit, onFocusHandled }: LimitsPartProps) {
  const { t } = useTranslation()
  const buttonRef = useFocusWhenOpenerIsGone<HTMLButtonElement>(focus, focus?.part === 'setLimit', onFocusHandled)

  return (
    <button
      ref={buttonRef}
      type="button"
      className="pressable mt-3 flex min-h-11 w-full items-center gap-3 rounded-[20px] bg-card px-4 py-2.5 text-left text-card-foreground hover:bg-secondary-hover"
      onClick={onSetLimit}
    >
      <span aria-hidden="true" className="text-[1.375rem] leading-none">
        {targetEmoji}
      </span>
      <span className="min-w-0 flex-1 font-semibold break-words">{t('budget.limits.setLimit')}</span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </button>
  )
}

function LimitsCardWithCategoryNames(props: LimitsPartProps) {
  const { t } = useTranslation()
  const categoriesQuery = usePersonalCategoriesWithArchived()

  if (categoriesQuery.isPending) {
    return (
      <KvitLoading>
        <KvitSkeleton className="mt-3 h-32 rounded-[20px]" />
      </KvitLoading>
    )
  }
  if (categoriesQuery.isError) {
    return (
      <KvitError message={t(errorMessageKey(categoriesQuery.error))} onRetry={() => void categoriesQuery.refetch()} />
    )
  }
  return <LimitsCard {...props} categories={categoriesQuery.data} />
}

interface LimitsCardProps extends LimitsPartProps {
  categories: readonly PersonalCategory[]
}

function LimitsCard({ limits, categories, focus, onOpenLimit, onSetLimit, onFocusHandled }: LimitsCardProps) {
  const { t } = useTranslation()
  const headingId = useId()
  const setLimitRef = useFocusWhenOpenerIsGone<HTMLButtonElement>(focus, focus?.part === 'setLimit', onFocusHandled)
  const monthRow = limits.month

  return (
    <section aria-labelledby={headingId} className={cn(balancesCardLook, 'mt-3 flex flex-col gap-1')}>
      <h2 id={headingId} className="text-[0.8125rem] font-bold text-muted-foreground">
        {t('budget.limits.title')}
      </h2>
      <ul className="-mx-3 flex flex-col gap-0.5">
        {monthRow !== null && (
          <LimitListItem
            row={monthRow}
            category={null}
            focus={focus}
            isFocusTarget={focus?.part === 'row' && focus.categoryId === null}
            onOpen={() => onOpenLimit({ category: null, limitMinor: monthRow.limitMinor })}
            onFocusHandled={onFocusHandled}
          />
        )}
        {limits.categories.map((row) => {
          const category = categoryOfLimit(row, categories)
          return (
            <LimitListItem
              key={category.id}
              row={row}
              category={category}
              focus={focus}
              isFocusTarget={focus?.part === 'row' && focus.categoryId === category.id}
              onOpen={() => onOpenLimit({ category, limitMinor: row.limitMinor })}
              onFocusHandled={onFocusHandled}
            />
          )
        })}
      </ul>
      <KvitButton ref={setLimitRef} variant="smallLink" className="w-auto self-center px-3" onClick={onSetLimit}>
        {t('budget.limits.setLimit')}
      </KvitButton>
    </section>
  )
}

interface LimitListItemProps {
  row: LimitRow
  category: PersonalCategory | null
  focus: LimitFocus | null
  isFocusTarget: boolean
  onOpen: () => void
  onFocusHandled: () => void
}

function LimitListItem({ row, category, focus, isFocusTarget, onOpen, onFocusHandled }: LimitListItemProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const buttonRef = useFocusWhenOpenerIsGone<HTMLButtonElement>(focus, isFocusTarget, onFocusHandled)
  const name = category === null ? t('budget.limits.wholeMonth') : personalCategoryName(category, t)
  const state = limitStateOf(row)

  return (
    <li>
      <button
        ref={buttonRef}
        type="button"
        className="pressable flex min-h-11 w-full flex-col gap-1.5 rounded-2xl px-3 py-2.5 text-left hover:bg-secondary-hover"
        onClick={onOpen}
      >
        <span className="flex w-full flex-wrap items-baseline gap-x-3 gap-y-0.5">
          <span className="flex min-w-0 flex-1 basis-32 items-baseline gap-2">
            <span aria-hidden="true" className="flex-none">
              {category === null ? wholeMonthEmoji : category.emoji}
            </span>
            <span data-limit-name className="min-w-0 font-semibold break-words">
              {name}
            </span>
          </span>
          <span data-limit-amounts className={cn('ml-auto text-right font-bold', state === 'over' && 'text-negative')}>
            <KvitAmount parts={moneyParts(row.spentMinor, 'MKD', language)} />
            {' / '}
            <KvitAmount parts={moneyParts(row.limitMinor, 'MKD', language)} />
          </span>
        </span>
        <span
          role="progressbar"
          aria-label={name}
          aria-valuemin={0}
          aria-valuenow={Math.min(row.spentMinor, row.limitMinor)}
          aria-valuemax={row.limitMinor}
          data-limit-state={state}
          className="block h-2 w-full overflow-hidden rounded-full bg-background"
        >
          <span
            data-limit-fill
            className={cn(
              'block h-full origin-left rounded-full transition-transform duration-200 ease-out motion-reduce:transition-none',
              fillLooks[state],
            )}
            style={{ transform: `scaleX(${limitFillRatio(row)})` }}
          />
        </span>
        {row.overMinor > 0 && (
          <span data-limit-over className="text-[0.8125rem] font-semibold text-negative">
            {t('budget.limits.over', { amount: formatMoney(row.overMinor, 'MKD', language) })}
          </span>
        )}
        {row.includedEurMinor > 0 && (
          <span data-limit-converted className="text-[0.8125rem] text-muted-foreground">
            {t('budget.limits.includesConverted', { amount: formatMoney(row.includedEurMinor, 'EUR', language) })}
          </span>
        )}
      </button>
    </li>
  )
}

function categoryOfLimit(row: LimitRow, categories: readonly PersonalCategory[]): PersonalCategory {
  const category = categories.find((candidate) => candidate.id === row.categoryId)
  if (category === undefined) {
    throw new Error(`The category ${row.categoryId ?? 'null'} of a limit is not one of the personal categories`)
  }
  return category
}
