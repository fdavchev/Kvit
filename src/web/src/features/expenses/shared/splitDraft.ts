import type { ExpenseShare, SplitShareInput, SplitType } from '@/core/services/expenses/expensesService'
import type { Currency } from '@/shared/utils/formatMoney'
import { hundredthsToText, moneyInputText, parseHundredths } from '@/shared/utils/parseMoneyInput'

export interface SplitDraftPerson {
  memberId: string
  isIn: boolean
  typedValue: string
  isTyped: boolean
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
    people: memberIds.map((memberId) => ({ memberId, isIn: true, typedValue: '', isTyped: false })),
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
      const typedValue = share === undefined ? '' : typedValueOf(splitType, share.inputValue, currency)
      return { memberId, isIn: share !== undefined, typedValue, isTyped: isTypedText(typedValue) }
    }),
  }
}

export function withSplitType(draft: SplitDraft, splitType: SplitType): SplitDraft {
  return {
    splitType,
    people: draft.people.map((person) => ({
      ...person,
      typedValue: person.isIn ? startingValueOf(splitType) : '',
      isTyped: false,
    })),
  }
}

export function withPersonAdded(draft: SplitDraft, memberId: string): SplitDraft {
  return {
    ...draft,
    people: [
      ...draft.people,
      { memberId, isIn: true, typedValue: startingValueOf(draft.splitType), isTyped: false },
    ],
  }
}

export function withoutPerson(draft: SplitDraft, memberId: string): SplitDraft {
  return { ...draft, people: draft.people.filter((person) => person.memberId !== memberId) }
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
      person.memberId === memberId ? { ...person, typedValue, isTyped: isTypedText(typedValue) } : person,
    ),
  }
}

export function withTypedValueAutoFilled(
  draft: SplitDraft,
  memberId: string,
  typedValue: string,
  amountMinor: number,
  currency: Currency,
): SplitDraft {
  const typed = withTypedValue(draft, memberId, typedValue)
  const typedPerson = typed.people.find((person) => person.memberId === memberId)
  if (typedPerson === undefined) {
    throw new Error(`The split has no person with the member id ${memberId}`)
  }
  if (!autoFillsSplitType(typed.splitType)) {
    return typed
  }
  return withRestFilledFrom(typed, typedPerson, amountMinor, currency)
}

export function withAutoFillRefreshed(draft: SplitDraft, amountMinor: number, currency: Currency): SplitDraft {
  if (draft.splitType !== 'Exact') {
    return draft
  }
  const typedPeople = draft.people.filter((person) => person.isIn && person.isTyped)
  if (typedPeople.length !== 1) {
    return draft
  }
  return withRestFilledFrom(draft, typedPeople[0], amountMinor, currency)
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

function autoFillsSplitType(splitType: SplitType): boolean {
  return splitType === 'Exact' || splitType === 'Percentage'
}

function withRestFilledFrom(
  draft: SplitDraft,
  typedPerson: SplitDraftPerson,
  amountMinor: number,
  currency: Currency,
): SplitDraft {
  const peopleIn = draft.people.filter((person) => person.isIn)
  if (!typedPerson.isIn || peopleIn.length !== 2) {
    return draft
  }
  const otherPerson = peopleIn.find((person) => person.memberId !== typedPerson.memberId)
  if (otherPerson === undefined || otherPerson.isTyped) {
    return draft
  }
  const restText = restTextOf(draft.splitType, typedPerson.typedValue, amountMinor, currency)
  return {
    ...draft,
    people: draft.people.map((person) =>
      person.memberId === otherPerson.memberId ? { ...person, typedValue: restText } : person,
    ),
  }
}

function restTextOf(splitType: SplitType, typedValue: string, amountMinor: number, currency: Currency): string {
  if (!isTypedText(typedValue)) {
    return ''
  }
  const typedNumber = parseTypedValue(splitType, typedValue, currency)
  if (typedNumber === null) {
    return ''
  }
  switch (splitType) {
    case 'Exact':
      return amountMinor === 0 || typedNumber > amountMinor
        ? ''
        : moneyInputText(amountMinor - typedNumber, currency)
    case 'Percentage':
      return typedNumber > wholePercentInHundredths ? '' : percentText(wholePercentInHundredths - typedNumber)
    case 'Equal':
    case 'Shares':
      throw new Error(`The ${splitType} split has no rest to fill in`)
  }
}

function isTypedText(typedValue: string): boolean {
  return typedValue.trim() !== ''
}

function startingValueOf(splitType: SplitType): string {
  return splitType === 'Shares' ? startingShares : ''
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
