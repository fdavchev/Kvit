import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { splitTypes, type SplitType } from '@/core/services/expenses/expensesService'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { cn } from '@/shared/utils/cn'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { formatUngroupedDecimal } from '@/shared/utils/formatNumber'
import { personOf, type ExpensePerson } from '../expensePeople'
import {
  percentText,
  splitProgress,
  withPersonIn,
  withSplitType,
  withTypedValueAutoFilled,
  type SplitDraft,
  type SplitDraftPerson,
  type SplitProgress,
} from '../splitDraft'
import { Tick } from './Tick'

interface SplitSheetProps {
  people: readonly ExpensePerson[]
  draft: SplitDraft
  amountMinor: number
  currency: Currency
  language: Language
  onChange: (draft: SplitDraft) => void
  onClose: () => void
}

const splitTabKeys: Record<SplitType, string> = {
  Equal: 'expense.splitEqual',
  Exact: 'expense.splitExact',
  Percentage: 'expense.splitPercentage',
  Shares: 'expense.splitShares',
}

const smallFieldLook =
  'min-h-11 min-w-11 w-24 rounded-[10px] border border-field-border bg-field px-2.5 text-right font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0'

const stepButtonLook = 'pressable grid min-h-11 min-w-11 place-items-center rounded-full'

const stepCircleLook =
  'grid size-9 place-items-center rounded-full border border-field-border bg-field text-lg text-field-foreground'

export function SplitSheet({
  people,
  draft,
  amountMinor,
  currency,
  language,
  onChange,
  onClose,
}: SplitSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [openedExtras, setOpenedExtras] = useState<readonly string[]>([])
  const progress = splitProgress(draft, amountMinor, currency)
  const moneyInputMode = currency === 'EUR' ? 'decimal' : 'numeric'

  function switchTab(splitType: SplitType): void {
    setOpenedExtras([])
    onChange(withSplitType(draft, splitType))
  }

  function typeValue(memberId: string, typedValue: string): void {
    onChange(withTypedValueAutoFilled(draft, memberId, typedValue, amountMinor, currency))
  }

  function renderPersonName(person: ExpensePerson) {
    return (
      <>
        <KvitAvatar name={person.name} pictureUrl={person.pictureUrl} colorIndex={person.colorIndex} />
        <span className="min-w-0 flex-1 font-semibold break-words">{person.name}</span>
      </>
    )
  }

  function renderEqualRow(draftPerson: SplitDraftPerson, person: ExpensePerson) {
    const showsExtraField: boolean =
      draftPerson.typedValue !== '' || openedExtras.includes(draftPerson.memberId)
    return (
      <>
        <button
          type="button"
          aria-pressed={draftPerson.isIn}
          onClick={() => onChange(withPersonIn(draft, draftPerson.memberId, !draftPerson.isIn))}
          className="pressable flex min-h-11 min-w-11 flex-1 items-center gap-3 rounded-xl text-left"
        >
          {renderPersonName(person)}
          <Tick isOn={draftPerson.isIn} />
        </button>
        {draftPerson.isIn &&
          (showsExtraField ? (
            <input
              type="text"
              inputMode={moneyInputMode}
              aria-label={t('expense.extra')}
              value={draftPerson.typedValue}
              autoFocus={draftPerson.typedValue === ''}
              onChange={(event) => typeValue(draftPerson.memberId, event.target.value)}
              className={smallFieldLook}
            />
          ) : (
            <button
              type="button"
              onClick={() => setOpenedExtras([...openedExtras, draftPerson.memberId])}
              className="pressable flex min-h-11 min-w-11 flex-none items-center rounded-full"
            >
              <span className="grid min-h-9 place-items-center rounded-full border border-field-border bg-field px-2.5 text-[0.875rem] font-medium text-muted-foreground">
                {t('expense.extra')}
              </span>
            </button>
          ))}
      </>
    )
  }

  function renderTypedRow(draftPerson: SplitDraftPerson, person: ExpensePerson) {
    const isPercentage: boolean = draft.splitType === 'Percentage'
    return (
      <>
        {renderPersonName(person)}
        <input
          type="text"
          inputMode={isPercentage ? 'decimal' : moneyInputMode}
          aria-label={person.name}
          value={draftPerson.typedValue}
          onChange={(event) => typeValue(draftPerson.memberId, event.target.value)}
          className={smallFieldLook}
        />
        {isPercentage && <span aria-hidden="true">%</span>}
      </>
    )
  }

  function renderSharesRow(draftPerson: SplitDraftPerson, person: ExpensePerson) {
    const shares: number = draftPerson.typedValue === '' ? 0 : Number(draftPerson.typedValue)
    return (
      <>
        {renderPersonName(person)}
        <span className="flex items-center gap-1.5">
          <button
            type="button"
            aria-label={t('expense.fewerShares', { name: person.name })}
            disabled={shares === 0}
            onClick={() => typeValue(draftPerson.memberId, String(shares - 1))}
            className={cn(stepButtonLook, 'disabled:opacity-45')}
          >
            <span aria-hidden="true" className={stepCircleLook}>
              −
            </span>
          </button>
          <span className="min-w-6 text-center font-bold">{shares}</span>
          <button
            type="button"
            aria-label={t('expense.moreShares', { name: person.name })}
            onClick={() => typeValue(draftPerson.memberId, String(shares + 1))}
            className={stepButtonLook}
          >
            <span aria-hidden="true" className={stepCircleLook}>
              +
            </span>
          </button>
        </span>
      </>
    )
  }

  function renderRow(draftPerson: SplitDraftPerson) {
    const person = personOf(people, draftPerson.memberId)
    switch (draft.splitType) {
      case 'Equal':
        return renderEqualRow(draftPerson, person)
      case 'Exact':
      case 'Percentage':
        return renderTypedRow(draftPerson, person)
      case 'Shares':
        return renderSharesRow(draftPerson, person)
    }
  }

  const shownPeople =
    draft.splitType === 'Equal' ? draft.people : draft.people.filter((person) => person.isIn)

  return (
    <KvitSheet title={t('expense.split')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div role="tablist" className="flex gap-0.5 rounded-full bg-track p-1">
        {splitTypes.map((splitType) => (
          <button
            key={splitType}
            type="button"
            role="tab"
            aria-selected={draft.splitType === splitType}
            onClick={() => switchTab(splitType)}
            className="pressable min-h-11 flex-1 rounded-full font-bold text-muted-foreground aria-selected:bg-pill aria-selected:text-foreground aria-selected:shadow-[0_1px_3px_var(--track-shadow)]"
          >
            {t(splitTabKeys[splitType])}
          </button>
        ))}
      </div>
      <div className="-mx-1 flex max-h-[45dvh] flex-col overflow-y-auto px-1">
        {shownPeople.map((draftPerson) => (
          <div
            key={draftPerson.memberId}
            role="group"
            aria-label={personOf(people, draftPerson.memberId).name}
            className="flex min-h-13 items-center gap-2.5"
          >
            {renderRow(draftPerson)}
          </div>
        ))}
      </div>
      <SplitProgressText progress={progress} currency={currency} language={language} />
      <KvitButton disabled={progress.kind !== 'ok'} onClick={sheet.close}>
        {t('expense.done')}
      </KvitButton>
    </KvitSheet>
  )
}

interface SplitProgressTextProps {
  progress: SplitProgress
  currency: Currency
  language: Language
}

function SplitProgressText({ progress, currency, language }: SplitProgressTextProps) {
  const { t } = useTranslation()
  if (progress.kind === 'amountLeft') {
    return (
      <p className="font-bold text-destructive">
        {t('expense.amountLeft', { amount: formatMoney(progress.minor, currency, language) })}
      </p>
    )
  }
  if (progress.kind === 'percentLeft') {
    return (
      <p className="font-bold text-destructive">
        {t('expense.percentLeft', { percent: formatPercent(progress.hundredths, language) })}
      </p>
    )
  }
  return null
}

function formatPercent(hundredths: number, language: Language): string {
  const sign = hundredths < 0 ? '-' : ''
  return formatUngroupedDecimal(`${sign}${percentText(Math.abs(hundredths))}`, language)
}
