import type { Language } from '@/core/i18n/language'
import { formatMoney } from '@/shared/utils/formatMoney'
import {
  bojanUserId,
  dinnerExpenseId,
  hotelExpenseId,
  museumExpenseId,
  taxiExpenseId,
  untitledExpenseId,
} from './expenseTestData'
import { macedonianDay } from './expenseTestHelpers'
import { testGroupId } from './groupTestData'
import {
  anaMember,
  anaUserId,
  grandmaMember,
  petarMember,
  petarUserId,
} from './memberTestData'
import { testMe } from './testMe'

export const activityPath = `/api/groups/${testGroupId}/activity`

export interface ActivityChangeJson {
  field: string
  old: string
  new: string
}

export interface ActivityEventJson {
  id: string
  type: string
  actorUserId: string
  actorName: string
  expenseId: string | null
  memberId: string | null
  changes: ActivityChangeJson[] | null
  data: Record<string, unknown> | null
  createdAt: string
}

export interface SentenceCase {
  name: string
  event: ActivityEventJson
  key: string
  params: (language: Language) => Record<string, string>
}

export interface EditCase {
  field: string
  old: string
  new: string
  key: string
  params: (language: Language) => Record<string, string>
}

export const activityEventId = '0f000000-0000-4000-8000-000000000001'

export function numberEvents(events: readonly ActivityEventJson[]): ActivityEventJson[] {
  return events.map((event, index) => ({
    ...event,
    id: `0f000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
  }))
}

export function activityEventOf(changes: Partial<ActivityEventJson>): ActivityEventJson {
  return {
    id: activityEventId,
    type: 'GroupCreated',
    actorUserId: anaUserId,
    actorName: 'Ana',
    expenseId: null,
    memberId: null,
    changes: null,
    data: null,
    createdAt: '2026-10-06T09:55:00Z',
    ...changes,
  }
}

function money(amountMinor: number, currency: 'MKD' | 'EUR', language: Language): string {
  return formatMoney(amountMinor, currency, language)
}

function dayText(language: Language, date: string): string {
  return language === 'en' ? `${Number(date.slice(8))} Oct` : macedonianDay(date, false)
}

function categoryText(language: Language, key: string): string {
  const names: Record<Language, Record<string, string>> = {
    en: { food: 'Food & drinks', accommodation: 'Accommodation' },
    mk: { food: 'Храна и пијалоци', accommodation: 'Сместување' },
  }
  return names[language][key]
}

export const expenseAddedEvent: ActivityEventJson = activityEventOf({
  type: 'ExpenseAdded',
  expenseId: dinnerExpenseId,
  data: { title: 'Dinner', amountMinor: 240000, currency: 'MKD' },
})

export const sentenceCases: SentenceCase[] = [
  {
    name: 'GroupCreated',
    event: activityEventOf({ type: 'GroupCreated', data: { name: 'Greece trip' } }),
    key: 'activity.groupCreated',
    params: () => ({ name: 'Ana' }),
  },
  {
    name: 'GroupRenamed',
    event: activityEventOf({
      type: 'GroupRenamed',
      changes: [{ field: 'name', old: 'Greece', new: 'Greece trip' }],
    }),
    key: 'activity.groupRenamed',
    params: () => ({ name: 'Ana', old: 'Greece', new: 'Greece trip' }),
  },
  {
    name: 'GroupSettingsChanged with a new emoji',
    event: activityEventOf({
      type: 'GroupSettingsChanged',
      changes: [{ field: 'emoji', old: '\u{1F3D6}\u{FE0F}', new: '\u{1F3E0}' }],
    }),
    key: 'activity.groupEmojiChanged',
    params: () => ({ name: 'Ana', old: '\u{1F3D6}\u{FE0F}', new: '\u{1F3E0}' }),
  },
  {
    name: 'GroupSettingsChanged with a new currency',
    event: activityEventOf({
      type: 'GroupSettingsChanged',
      changes: [{ field: 'defaultCurrency', old: 'MKD', new: 'EUR' }],
    }),
    key: 'activity.groupCurrencyChanged',
    params: () => ({ name: 'Ana', old: 'MKD', new: 'EUR' }),
  },
  {
    name: 'InviteLinkReset',
    event: activityEventOf({ type: 'InviteLinkReset' }),
    key: 'activity.inviteLinkReset',
    params: () => ({ name: 'Ana' }),
  },
  {
    name: 'InviteLinkRestored',
    event: activityEventOf({ type: 'InviteLinkRestored' }),
    key: 'activity.inviteLinkRestored',
    params: () => ({ name: 'Ana' }),
  },
  {
    name: 'MemberAdded',
    event: activityEventOf({
      type: 'MemberAdded',
      memberId: grandmaMember.id,
      data: { name: 'Grandma' },
    }),
    key: 'activity.memberAdded',
    params: () => ({ name: 'Ana', member: 'Grandma' }),
  },
  {
    name: 'MemberJoined',
    event: activityEventOf({
      type: 'MemberJoined',
      actorUserId: petarUserId,
      actorName: 'Petar',
      memberId: petarMember.id,
      data: { name: 'Petar' },
    }),
    key: 'activity.memberJoined',
    params: () => ({ member: 'Petar' }),
  },
  {
    name: 'MemberClaimed',
    event: activityEventOf({
      type: 'MemberClaimed',
      actorUserId: petarUserId,
      actorName: 'Petar',
      memberId: petarMember.id,
      data: { name: 'Petar', claimedName: 'Darko' },
    }),
    key: 'activity.memberClaimed',
    params: () => ({ member: 'Petar', claimedName: 'Darko' }),
  },
  {
    name: 'ClaimUndone',
    event: activityEventOf({
      type: 'ClaimUndone',
      memberId: petarMember.id,
      data: { name: 'Petar', claimedName: 'Darko' },
    }),
    key: 'activity.claimUndone',
    params: () => ({ name: 'Ana', claimedName: 'Darko' }),
  },
  {
    name: 'MemberRemoved',
    event: activityEventOf({
      type: 'MemberRemoved',
      memberId: 'a1b2c3d4-0006-4aaa-8bbb-000000000006',
      data: { name: 'Bojan' },
    }),
    key: 'activity.memberRemoved',
    params: () => ({ name: 'Ana', member: 'Bojan' }),
  },
  {
    name: 'MemberLeft',
    event: activityEventOf({
      type: 'MemberLeft',
      actorUserId: bojanUserId,
      actorName: 'Bojan',
      memberId: 'a1b2c3d4-0006-4aaa-8bbb-000000000006',
      data: { name: 'Bojan' },
    }),
    key: 'activity.memberLeft',
    params: () => ({ member: 'Bojan' }),
  },
  {
    name: 'OwnershipTransferred',
    event: activityEventOf({
      type: 'OwnershipTransferred',
      memberId: anaMember.id,
      data: { name: 'Petar' },
    }),
    key: 'activity.ownershipTransferred',
    params: () => ({ name: 'Ana', member: 'Petar' }),
  },
  {
    name: 'MemberLetBackIn',
    event: activityEventOf({
      type: 'MemberLetBackIn',
      memberId: 'a1b2c3d4-0006-4aaa-8bbb-000000000006',
      data: { name: 'Bojan' },
    }),
    key: 'activity.memberLetBackIn',
    params: () => ({ name: 'Ana', member: 'Bojan' }),
  },
  {
    name: 'GroupDeleted',
    event: activityEventOf({ type: 'GroupDeleted' }),
    key: 'activity.groupDeleted',
    params: () => ({ name: 'Ana' }),
  },
  {
    name: 'GroupRestored',
    event: activityEventOf({ type: 'GroupRestored' }),
    key: 'activity.groupRestored',
    params: () => ({ name: 'Ana' }),
  },
  {
    name: 'ExpenseAdded with a title',
    event: expenseAddedEvent,
    key: 'activity.expenseAdded',
    params: (language) => ({ name: 'Ana', title: 'Dinner', amount: money(240000, 'MKD', language) }),
  },
  {
    name: 'ExpenseAdded without a title',
    event: activityEventOf({
      type: 'ExpenseAdded',
      expenseId: untitledExpenseId,
      data: { title: null, amountMinor: 4500, currency: 'EUR' },
    }),
    key: 'activity.expenseAddedNoTitle',
    params: (language) => ({ name: 'Ana', amount: money(4500, 'EUR', language) }),
  },
  {
    name: 'ExpenseDeleted with a title',
    event: activityEventOf({
      type: 'ExpenseDeleted',
      expenseId: museumExpenseId,
      data: { title: 'Museum tickets', amountMinor: 80000, currency: 'MKD' },
    }),
    key: 'activity.expenseDeleted',
    params: (language) => ({
      name: 'Ana',
      title: 'Museum tickets',
      amount: money(80000, 'MKD', language),
    }),
  },
  {
    name: 'ExpenseDeleted without a title',
    event: activityEventOf({
      type: 'ExpenseDeleted',
      expenseId: museumExpenseId,
      data: { title: null, amountMinor: 1250, currency: 'EUR' },
    }),
    key: 'activity.expenseDeletedNoTitle',
    params: (language) => ({ name: 'Ana', amount: money(1250, 'EUR', language) }),
  },
  {
    name: 'ExpenseRestored',
    event: activityEventOf({ type: 'ExpenseRestored', expenseId: taxiExpenseId }),
    key: 'activity.expenseRestored',
    params: () => ({ name: 'Ana' }),
  },
]

export const editCases: EditCase[] = [
  {
    field: 'amount',
    old: '280000',
    new: '300000',
    key: 'expense.historyChangedAmount',
    params: (language) => ({
      name: 'Ana',
      old: money(280000, 'MKD', language),
      new: money(300000, 'MKD', language),
    }),
  },
  {
    field: 'currency',
    old: 'EUR',
    new: 'MKD',
    key: 'expense.historyChangedCurrency',
    params: () => ({ name: 'Ana', old: 'EUR', new: 'MKD' }),
  },
  {
    field: 'title',
    old: '',
    new: 'Hotel',
    key: 'expense.historyChangedTitle',
    params: () => ({ name: 'Ana', old: '—', new: 'Hotel' }),
  },
  {
    field: 'note',
    old: 'Booked',
    new: '',
    key: 'expense.historyChangedNote',
    params: () => ({ name: 'Ana', old: 'Booked', new: '—' }),
  },
  {
    field: 'date',
    old: '2026-10-02',
    new: '2026-10-03',
    key: 'expense.historyChangedDate',
    params: (language) => ({
      name: 'Ana',
      old: dayText(language, '2026-10-02'),
      new: dayText(language, '2026-10-03'),
    }),
  },
  {
    field: 'category',
    old: 'food',
    new: 'accommodation',
    key: 'expense.historyChangedCategory',
    params: (language) => ({
      name: 'Ana',
      old: categoryText(language, 'food'),
      new: categoryText(language, 'accommodation'),
    }),
  },
  {
    field: 'paidBy',
    old: 'Ana',
    new: 'Filip',
    key: 'expense.historyChangedPaidBy',
    params: () => ({ name: 'Ana', old: 'Ana', new: 'Filip' }),
  },
  {
    field: 'split',
    old: 'Equal: Ana 600, Filip 600',
    new: 'Exact: Ana 400, Filip 800',
    key: 'expense.historyChangedSplit',
    params: () => ({ name: 'Ana' }),
  },
]

export function editedEventOf(
  changes: ActivityChangeJson[],
  expenseId: string = hotelExpenseId,
): ActivityEventJson {
  return activityEventOf({ type: 'ExpenseEdited', expenseId, changes })
}

export const filipActorUserId = testMe.id
