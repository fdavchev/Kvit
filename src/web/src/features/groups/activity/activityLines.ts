import type { ActivityEvent } from '@/core/services/activity/activityService'
import type { ExpenseChange, ExpenseListRow } from '@/core/services/expenses/expensesService'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readOneOf,
  readText,
  readTextOrNull,
  type Fields,
} from '@/core/services/readFields'
import { categoryOf } from '@/features/expenses/shared/expenseCategories'
import {
  currencyAfterOf,
  currencyBeforeOf,
  expenseChangeSentence,
  type ChangeSentenceContext,
} from '@/features/expenses/shared/expenseChangeSentence'
import { expenseTitle } from '@/features/expenses/shared/expenseTitle'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'

export interface ActivityContext extends ChangeSentenceContext {
  expenses: readonly ExpenseListRow[]
}

export interface ActivityLine {
  key: string
  actorUserId: string
  actorName: string
  sentence: string
  expenseTitle: string | null
  linkedExpenseId: string | null
  createdAt: string
}

export function activityLines(events: readonly ActivityEvent[], context: ActivityContext): ActivityLine[] {
  const { t } = context
  const currencyOfOlderEdit = new Map<string, Currency>()

  function expenseRowOf(event: ActivityEvent): ExpenseListRow | undefined {
    return context.expenses.find((row) => row.id === event.expenseId)
  }

  function money(data: Fields, what: string): string {
    return formatMoney(
      readCount(data, 'amountMinor', what),
      readOneOf(data, 'currency', groupCurrencies, what),
      context.language,
    )
  }

  function expenseFactsSentence(event: ActivityEvent, withTitleKey: string, withoutTitleKey: string): string {
    const what = `the data of the ${event.type} event`
    const data = dataOf(event)
    const title = readTextOrNull(data, 'title', what)
    const amount = money(data, what)
    return title === null
      ? t(withoutTitleKey, { name: event.actorName, amount })
      : t(withTitleKey, { name: event.actorName, title, amount })
  }

  function memberName(event: ActivityEvent, field: string): string {
    return readText(dataOf(event), field, `the data of the ${event.type} event`)
  }

  function groupSettingsSentence(event: ActivityEvent, change: ExpenseChange): string {
    const values = { name: event.actorName, old: change.old, new: change.new }
    switch (change.field) {
      case 'emoji':
        return t('activity.groupEmojiChanged', values)
      case 'defaultCurrency':
        return t('activity.groupCurrencyChanged', values)
      default:
        throw new Error(`The ${event.type} event changes a group field Kvit does not know: "${change.field}"`)
    }
  }

  function groupRenamedSentence(event: ActivityEvent, change: ExpenseChange): string {
    if (change.field !== 'name') {
      throw new Error(`The ${event.type} event changes a group field Kvit does not know: "${change.field}"`)
    }
    return t('activity.groupRenamed', { name: event.actorName, old: change.old, new: change.new })
  }

  function editSentences(event: ActivityEvent): string[] {
    const changes = changesOf(event)
    const expenseId = expenseIdOf(event)
    const currencyAfter = currencyAfterTheEdit(event, changes, expenseId)
    const currencyBefore = currencyBeforeOf(changes, currencyAfter)
    currencyOfOlderEdit.set(expenseId, currencyBefore)
    return changes.map((change) =>
      expenseChangeSentence(change, event.actorName, { before: currencyBefore, after: currencyAfter }, context),
    )
  }

  function currencyAfterTheEdit(event: ActivityEvent, changes: ExpenseChange[], expenseId: string): Currency {
    if (event.data !== null) {
      return readOneOf(event.data, 'currency', groupCurrencies, `the data of the ${event.type} event ${event.id}`)
    }
    const knownCurrency = currencyOfOlderEdit.get(expenseId) ?? expenseRowOf(event)?.currency
    if (knownCurrency === undefined) {
      throw new Error(
        `The ${event.type} event ${event.id} carries no currency and its expense ${expenseId} is not in the list, so its amounts cannot be shown`,
      )
    }
    return currencyAfterOf(changes, knownCurrency)
  }

  function sentencesOf(event: ActivityEvent): string[] {
    const name = event.actorName
    switch (event.type) {
      case 'GroupCreated':
        return [t('activity.groupCreated', { name })]
      case 'GroupRenamed':
        return changesOf(event).map((change) => groupRenamedSentence(event, change))
      case 'GroupSettingsChanged':
        return changesOf(event).map((change) => groupSettingsSentence(event, change))
      case 'InviteLinkReset':
        return [t('activity.inviteLinkReset', { name })]
      case 'InviteLinkRestored':
        return [t('activity.inviteLinkRestored', { name })]
      case 'MemberAdded':
        return [t('activity.memberAdded', { name, member: memberName(event, 'name') })]
      case 'MemberJoined':
        return [t('activity.memberJoined', { member: memberName(event, 'name') })]
      case 'MemberClaimed':
        return [
          t('activity.memberClaimed', {
            member: memberName(event, 'name'),
            claimedName: memberName(event, 'claimedName'),
          }),
        ]
      case 'ClaimUndone':
        return [t('activity.claimUndone', { name, claimedName: memberName(event, 'claimedName') })]
      case 'MemberRemoved':
        return [t('activity.memberRemoved', { name, member: memberName(event, 'name') })]
      case 'MemberLeft':
        return [t('activity.memberLeft', { member: memberName(event, 'name') })]
      case 'OwnershipTransferred':
        return [t('activity.ownershipTransferred', { name, member: memberName(event, 'name') })]
      case 'MemberLetBackIn':
        return [t('activity.memberLetBackIn', { name, member: memberName(event, 'name') })]
      case 'GroupDeleted':
        return [t('activity.groupDeleted', { name })]
      case 'GroupRestored':
        return [t('activity.groupRestored', { name })]
      case 'ExpenseAdded':
        return [expenseFactsSentence(event, 'activity.expenseAdded', 'activity.expenseAddedNoTitle')]
      case 'ExpenseDeleted':
        return [expenseFactsSentence(event, 'activity.expenseDeleted', 'activity.expenseDeletedNoTitle')]
      case 'ExpenseRestored':
        return [t('activity.expenseRestored', { name })]
      case 'ExpenseEdited':
        return editSentences(event)
      default:
        throw new Error(`The activity has an event type Kvit has no sentence for: "${event.type}"`)
    }
  }

  return events.flatMap((event) => {
    const expenseRow = expenseRowOf(event)
    const title =
      event.type === 'ExpenseEdited' && expenseRow !== undefined
        ? expenseTitle(expenseRow.title, categoryOf(context.categories, expenseRow.categoryId), t)
        : null
    return sentencesOf(event).map((sentence, index) => ({
      key: `${event.id}-${index}`,
      actorUserId: event.actorUserId,
      actorName: event.actorName,
      sentence,
      expenseTitle: title,
      linkedExpenseId: expenseRow === undefined ? null : expenseRow.id,
      createdAt: event.createdAt,
    }))
  })
}

function dataOf(event: ActivityEvent): Fields {
  if (event.data === null) {
    throw new Error(`The ${event.type} event ${event.id} has no data`)
  }
  return event.data
}

function changesOf(event: ActivityEvent): ExpenseChange[] {
  if (event.changes === null || event.changes.length === 0) {
    throw new Error(`The ${event.type} event ${event.id} has no changes`)
  }
  return event.changes
}

function expenseIdOf(event: ActivityEvent): string {
  if (event.expenseId === null) {
    throw new Error(`The ${event.type} event ${event.id} names no expense`)
  }
  return event.expenseId
}
