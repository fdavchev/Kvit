import type { CSSProperties, HTMLAttributes, ReactNode } from 'react'
import { cn } from '@/shared/utils/cn'
import { moneyText, type MoneyParts } from '@/shared/utils/formatMoney'
import { KvitRollingNumber } from './KvitAmount'

interface KvitFitAmountProps extends HTMLAttributes<HTMLSpanElement> {
  parts: MoneyParts
}

interface FitBoxProps extends HTMLAttributes<HTMLSpanElement> {
  parts: MoneyParts
  children: ReactNode
}

const unbrokenLook = 'whitespace-nowrap'

function FitBox({ parts, children, className, ...attributes }: FitBoxProps) {
  const lengthStyle = { '--amount-length': moneyText(parts).length } as CSSProperties

  return (
    <span className={cn('@container block w-full', className)} {...attributes}>
      <span className="fit-amount block" style={lengthStyle}>
        {children}
      </span>
    </span>
  )
}

function withoutLabel(parts: MoneyParts): MoneyParts {
  return { ...parts, currencyAfter: '' }
}

function labelOf(parts: MoneyParts): string {
  return parts.currencyAfter.trim()
}

function LabelAfterSpace({ label }: { label: string }) {
  if (label === '') {
    return null
  }
  return (
    <>
      {' '}
      <span className={unbrokenLook}>{label}</span>
    </>
  )
}

export function KvitFitAmount({ parts, ...attributes }: KvitFitAmountProps) {
  return (
    <FitBox parts={parts} {...attributes}>
      <span className={unbrokenLook}>{moneyText(withoutLabel(parts))}</span>
      <LabelAfterSpace label={labelOf(parts)} />
    </FitBox>
  )
}

export function KvitFitRollingAmount({ parts, ...attributes }: KvitFitAmountProps) {
  return (
    <FitBox parts={parts} {...attributes}>
      <span className="sr-only">{moneyText(parts)}</span>
      <span aria-hidden="true">
        <KvitRollingNumber parts={withoutLabel(parts)} />
        <LabelAfterSpace label={labelOf(parts)} />
      </span>
    </FitBox>
  )
}
