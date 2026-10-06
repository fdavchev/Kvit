import type { ExpenseShare, SplitShareInput, SplitType } from '@/core/services/expenses/expensesService'
import type { Currency } from '@/shared/utils/formatMoney'
import { hundredthsToText, moneyInputText, parseHundredths } from '@/shared/utils/parseMoneyInput'

export interface SplitDraftPerson {
  memberId: string
  isIn: boolean
  typedValue: string
}

export interface SplitDraft {
  splitType: SplitType
  people: SplitDraftPerson[]
}

export type SplitProgress =
  | { kind: 'ok' }
  | { kind: 'amountLeft'; minor: number }
  | { kind: 'percentLeft'; hundredths: number }
  | { kind: 'noShares' }
  | { kind: 'notANumber' }

const wholePercentInHundredths = 10000
const startingShares = '1'

export function defaultSplitDraft(memberIds: readonly string[]): SplitDraft {
  return {
    splitType: 'Equal',
    people: memberIds.map((memberId) => ({ memberId, isIn: true, typedValue: '' })),
  }
}

export function draftFromShares(
  splitType: SplitType,
  memberIds: readonly string[],
  shares: readonly ExpenseShare[],
  currency: Currency,
): SplitDraft {
  return {
    splitType,
    people: memberIds.map((memberId) => {
      const share = shares.find((candidate) => candidate.memberId === memberId)
      return {
        memberId,
        isIn: share !== undefined,
        typedValue: share === undefined ? '' : typedValueOf(splitType, share.inputValue, currency),
      }
    }),
  }
}

export function withSplitType(draft: SplitDraft, splitType: SplitType): SplitDraft {
  return {
    splitType,
    people: draft.people.map((person) => ({
      ...person,
      typedValue: splitType === 'Shares' && person.isIn ? startingShares : '',
    })),
  }
}

export function withPersonIn(draft: SplitDraft, memberId: string, isIn: boolean): SplitDraft {
  return {
    ...draft,
    people: draft.people.map((person) =>
      person.memberId === memberId ? { ...person, isIn } : person,
    ),
  }
}

export function withTypedValue(draft: SplitDraft, memberId: string, typedValue: string): SplitDraft {
  return {
    ...draft,
    people: draft.people.map((person) =>
      person.memberId === memberId ? { ...person, typedValue } : person,
    ),
  }
}

export function splitProgress(
  draft: SplitDraft,
  amountMinor: number,
  currency: Currency,
): SplitProgress {
  const peopleIn = draft.people.filter((person) => person.isIn)
  if (peopleIn.length === 0) {
    return { kind: 'noShares' }
  }
  let total = 0
  for (const person of peopleIn) {
    const value = parseTypedValue(draft.splitType, person.typedValue, currency)
    if (value === null) {
      return { kind: 'notANumber' }
    }
    total += value
  }
  switch (draft.splitType) {
    case 'Equal':
      return total <= amountMinor ? { kind: 'ok' } : { kind: 'amountLeft', minor: amountMinor - total }
    case 'Exact':
      return total === amountMinor ? { kind: 'ok' } : { kind: 'amountLeft', minor: amountMinor - total }
    case 'Percentage':
      return total === wholePercentInHundredths
        ? { kind: 'ok' }
        : { kind: 'percentLeft', hundredths: wholePercentInHundredths - total }
    case 'Shares':
      return total > 0 ? { kind: 'ok' } : { kind: 'noShares' }
  }
}

export function buildSplitRequest(draft: SplitDraft, currency: Currency): SplitShareInput[] {
  return draft.people
    .filter((person) => person.isIn)
    .map((person) => {
      const inputValue = parseTypedValue(draft.splitType, person.typedValue, currency)
      if (inputValue === null) {
        throw new Error(
          `The ${draft.splitType} split has a typed value that is not a number: "${person.typedValue}"`,
        )
      }
      return { memberId: person.memberId, inputValue }
    })
}

export function percentText(hundredths: number): string {
  return hundredthsToText(hundredths, false)
}

function parseTypedValue(splitType: SplitType, typedValue: string, currency: Currency): number | null {
  if (typedValue.trim() === '') {
    return 0
  }
  switch (splitType) {
    case 'Equal':
    case 'Exact':
      return parseHundredths(typedValue, currency === 'EUR')
    case 'Percentage':
      return parseHundredths(typedValue, true)
    case 'Shares':
      return /^\d+$/.test(typedValue.trim()) ? Number(typedValue.trim()) : null
  }
}

function typedValueOf(splitType: SplitType, inputValue: number, currency: Currency): string {
  switch (splitType) {
    case 'Equal':
      return inputValue === 0 ? '' : moneyInputText(inputValue, currency)
    case 'Exact':
      return moneyInputText(inputValue, currency)
    case 'Percentage':
      return percentText(inputValue)
    case 'Shares':
      return String(inputValue)
  }
}
