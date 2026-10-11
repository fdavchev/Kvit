import type { TFunction } from 'i18next'
import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import type { Language } from '@/core/i18n/language'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { cn } from '@/shared/utils/cn'
import { formatMoney } from '@/shared/utils/formatMoney'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { amountLookOf, balancesCardLook } from '../balancesLooks'
import type { BalanceCardRow, BalanceCardView, OwedLine } from '../balancesView'

interface BalanceCardProps {
  title: string
  card: BalanceCardView
  language: Language
}

interface RowTexts {
  title: string
  detail: string
}

export function BalanceCard({ title, card, language }: BalanceCardProps) {
  const { t } = useTranslation()
  const headingId = useId()

  function rowTexts(row: BalanceCardRow): RowTexts {
    const amount = formatMoney(row.amountMinor, card.currency, language)
    switch (row.kind) {
      case 'youPay':
        return { title: t('balances.youPay', { name: row.other.name }), detail: amount }
      case 'paysYou':
        return { title: t('balances.paysYou', { name: row.other.name }), detail: amount }
      case 'youPaid':
        return {
          title: t('balances.youPaid', { name: row.other.name, amount }),
          detail: t('balances.waitingFor', { name: row.other.name }),
        }
    }
  }

  return (
    <section aria-labelledby={headingId} className={cn(balancesCardLook, 'mb-3.5 flex flex-col gap-2.5')}>
      <div className="flex flex-col items-center text-center">
        <h2 id={headingId} className="text-[0.8125rem] font-normal text-muted-foreground">
          {title}
        </h2>
        <p
          className={cn(
            'text-[1.6rem] leading-tight font-extrabold tracking-[-0.02em] whitespace-nowrap',
            amountLookOf(card.myBalanceMinor),
          )}
        >
          {formatSignedMoney(card.myBalanceMinor, card.currency, language)}
        </p>
        <p className="text-[0.8125rem] text-muted-foreground">{owedLineText(card.owedLine, t, language)}</p>
      </div>
      {card.rows.length > 0 && (
        <ul className="flex flex-col gap-2.5">
          {card.rows.map((row) => {
            const texts = rowTexts(row)
            return (
              <li key={row.key} className="flex items-center gap-3 border-t border-field-border pt-2.5">
                <KvitAvatar
                  name={row.other.name}
                  pictureUrl={row.other.pictureUrl}
                  colorIndex={row.other.colorIndex}
                />
                <div className="flex min-w-0 flex-1 flex-col">
                  <p className="font-bold break-words">{texts.title}</p>
                  <p className="text-[0.8125rem] text-muted-foreground">{texts.detail}</p>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

function owedLineText(line: OwedLine, t: TFunction, language: Language): string {
  switch (line.kind) {
    case 'allEven':
      return t('balances.allEven')
    case 'youOwe':
      return namesText(line.names, 'balances.youOweOne', 'balances.youOweTwo', 'balances.youOweMany', t, language)
    case 'oweYou':
      return namesText(line.names, 'balances.owesYouOne', 'balances.oweYouTwo', 'balances.oweYouMany', t, language)
  }
}

function namesText(
  names: readonly string[],
  oneKey: string,
  twoKey: string,
  manyKey: string,
  t: TFunction,
  language: Language,
): string {
  const [first, second] = names
  if (first === undefined) {
    throw new Error('Expected at least one name for the line under the balance, found none')
  }
  if (second === undefined) {
    return t(oneKey, { name: first })
  }
  if (names.length === 2) {
    return t(twoKey, { first, second })
  }
  return countedText(t, manyKey, names.length - 2, language, { first, second })
}
