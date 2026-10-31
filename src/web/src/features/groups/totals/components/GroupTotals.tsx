import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Category } from '@/core/services/categories/categoriesService'
import type { CurrencySpending, GroupSpending } from '@/core/services/groups/groupSpendingService'
import type { Group } from '@/core/services/groups/groupsService'
import { categoryOf } from '@/features/expenses/shared/expenseCategories'
import { CategoryTile } from '@/features/expenses/shared/components/CategoryTile'
import { useCategories } from '@/features/expenses/shared/hooks/useCategories'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { categoryNameKey } from '@/shared/utils/categoryLooks'
import { cn } from '@/shared/utils/cn'
import { currencyLabel, moneyParts } from '@/shared/utils/formatMoney'
import { balancesCardLook } from '../../balances/balancesLooks'
import { GroupPlan } from '../../plan/components/GroupPlan'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { useGroupSpending } from '../hooks/useGroupSpending'

const chartEmoji = '\u{1F4CA}'
const noCategoryEmoji = '\u{1F3F7}\u{FE0F}'

interface GroupTotalsProps {
  group: Group
}

export function GroupTotals({ group }: GroupTotalsProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const spendingQuery = useGroupSpending(group.id)
  const needsCategories: boolean = spendingQuery.data !== undefined && hasAnyCategory(spendingQuery.data)
  const categoriesQuery = useCategories(needsCategories)

  if (spendingQuery.isError) {
    return <GroupLoadError error={spendingQuery.error} onRetry={() => void spendingQuery.refetch()} />
  }
  if (needsCategories && categoriesQuery.isError) {
    return <GroupLoadError error={categoriesQuery.error} onRetry={() => void categoriesQuery.refetch()} />
  }
  if (spendingQuery.isPending || (needsCategories && categoriesQuery.isPending)) {
    return (
      <KvitLoading>
        <div className="flex flex-col gap-2 pb-6">
          <KvitSkeleton className="mb-1.5 h-26 rounded-[20px]" />
          <KvitSkeleton className="h-14" />
          <KvitSkeleton className="h-14" />
        </div>
      </KvitLoading>
    )
  }
  const { currencies, plan } = spendingQuery.data
  const planPart = <GroupPlan group={group} plan={plan} />
  if (currencies.length === 0) {
    return (
      <>
        {planPart}
        <KvitEmpty emoji={chartEmoji} title={t('groupTotals.emptyTitle')} message={t('groupTotals.emptyHint')} />
      </>
    )
  }
  const categories: readonly Category[] = categoriesQuery.data ?? []
  return (
    <div className="flex flex-col pb-6">
      {planPart}
      <h2 className="mx-1 mb-2 text-[0.8125rem] font-bold text-muted-foreground">{t('groupTotals.title')}</h2>
      {currencies.map((spending) => (
        <CurrencyTotals key={spending.currency} spending={spending} categories={categories} language={language} />
      ))}
    </div>
  )
}

interface CurrencyTotalsProps {
  spending: CurrencySpending
  categories: readonly Category[]
  language: Language
}

function CurrencyTotals({ spending, categories, language }: CurrencyTotalsProps) {
  const { t } = useTranslation()
  const headingId = useId()

  return (
    <section aria-labelledby={headingId} className={cn(balancesCardLook, 'mb-3.5 flex flex-col gap-2.5')}>
      <div className="flex flex-col items-center text-center">
        <h3 id={headingId} className="text-[0.8125rem] font-normal text-muted-foreground">
          {currencyLabel(spending.currency, language)}
        </h3>
        <p className="text-[1.6rem] leading-tight font-extrabold tracking-[-0.02em] whitespace-nowrap lg:text-[clamp(2rem,3.5vw,3rem)] lg:font-black">
          <KvitAmount parts={moneyParts(spending.totalMinor, spending.currency, language)} />
        </p>
      </div>
      <ul className="flex flex-col gap-2.5">
        {spending.categories.map((row) => {
          const category = categoryOf(categories, row.categoryId)
          return (
            <li key={row.categoryId ?? 'none'} className="flex flex-wrap items-center gap-3 border-t border-field-border pt-2.5">
              {category === null ? (
                <KvitEmojiTile emoji={noCategoryEmoji} variant="dashed" />
              ) : (
                <CategoryTile category={category} />
              )}
              <span className="min-w-0 flex-1 basis-20 font-semibold break-words">
                {category === null ? t('groupTotals.noCategory') : t(categoryNameKey(category))}
              </span>
              <span className="ml-auto font-bold whitespace-nowrap">
                <KvitAmount parts={moneyParts(row.totalMinor, spending.currency, language)} />
              </span>
            </li>
          )
        })}
      </ul>
    </section>
  )
}

function hasAnyCategory(spending: GroupSpending): boolean {
  return spending.currencies.some((currency) => currency.categories.some((row) => row.categoryId !== null))
}
