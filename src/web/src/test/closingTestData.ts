import type { Language } from '@/core/i18n/language'
import type { Group } from '@/core/services/groups/groupsService'
import { groupPath } from './expenseTestHelpers'
import { groupOf } from './groupTestData'
import { anaMember, petarMember } from './memberTestData'

export const closingStartedAt = '2026-10-06T08:30:00Z'
export const closingEndsAt = '2026-10-07T08:30:00Z'

export const closingEndDay: Record<Language, string> = { en: '7 Oct', mk: '7 окт' }

export const closingPath = `${groupPath}/closing`
export const closingConfirmPath = `${groupPath}/closing/confirm`
export const closingObjectPath = `${groupPath}/closing/object`
export const unlockPath = `${groupPath}/unlock`

export const anaClosingRow = { id: anaMember.id, name: anaMember.displayName }
export const petarClosingRow = { id: petarMember.id, name: petarMember.displayName }

const closingTimes = { closingStartedAt, closingEndsAt }

export const readyToCloseGroup: Group = groupOf({ memberCount: 5, canStartClosing: true })

export const closingGroup: Group = groupOf({
  memberCount: 5,
  status: 'Closing',
  ...closingTimes,
  closingConfirmedBy: [],
  closingWaitingFor: [anaClosingRow, petarClosingRow],
})

export const closingGroupAnaConfirmed: Group = groupOf({
  memberCount: 5,
  status: 'Closing',
  ...closingTimes,
  closingConfirmedBy: [anaClosingRow],
  closingWaitingFor: [petarClosingRow],
})

export const finishedGroup: Group = groupOf({
  memberCount: 5,
  status: 'Finished',
  ...closingTimes,
  closingConfirmedBy: [anaClosingRow, petarClosingRow],
  closingWaitingFor: [],
})

export const finishedGroupWithoutAnswers: Group = groupOf({
  memberCount: 1,
  status: 'Finished',
  ...closingTimes,
  closingConfirmedBy: [],
  closingWaitingFor: [],
})

export function asMemberOf(group: Group): Group {
  return { ...group, isOwner: false }
}

export interface ClosingText {
  key: string
  en: string
  mk: string
}

export const closingTexts = {
  readyTitle: {
    key: 'closing.readyTitle',
    en: "Everyone's kvit – close the group?",
    mk: 'Сите се квит – да ја затвориме групата?',
  },
  checkBalances: {
    key: 'closing.checkBalances',
    en: 'All balances are 0',
    mk: 'Сите салда се 0',
  },
  checkPayments: {
    key: 'closing.checkPayments',
    en: 'No payment is waiting',
    mk: 'Ниедно плаќање не чека',
  },
  start: {
    key: 'closing.start',
    en: 'Close the group',
    mk: 'Затвори ја групата',
  },
  waitingTitle: {
    key: 'closing.waitingTitle',
    en: 'Closing · waiting for others',
    mk: 'Се затвора · чека на другите',
  },
  personConfirmed: {
    key: 'closing.personConfirmed',
    en: '{{name}} confirmed',
    mk: '{{name}} потврди',
  },
  personWaiting: {
    key: 'closing.personWaiting',
    en: '{{name}} has not answered',
    mk: '{{name}} уште не одговори',
  },
  changeCancels: {
    key: 'closing.changeCancels',
    en: 'Changing an expense or a payment cancels it.',
    mk: 'Ако се промени трошок или плаќање, затворањето се откажува.',
  },
  prompt: {
    key: 'closing.prompt',
    en: '{{name}} wants to close “{{group}}”',
    mk: '{{name}} сака да ја затвори групата „{{group}}“',
  },
  object: {
    key: 'closing.object',
    en: 'Object',
    mk: 'Приговори',
  },
  youConfirmed: {
    key: 'closing.youConfirmed',
    en: 'You confirmed',
    mk: 'Ти потврди',
  },
  waitingFor: {
    key: 'closing.waitingFor',
    en: 'Waiting for {{names}}',
    mk: 'Се чека {{names}}',
  },
  objectTitle: {
    key: 'closing.objectTitle',
    en: 'Object to closing',
    mk: 'Приговор на затворањето',
  },
  reasonLabel: {
    key: 'closing.reasonLabel',
    en: 'Reason (optional)',
    mk: 'Причина (по желба)',
  },
  send: {
    key: 'closing.send',
    en: 'Send',
    mk: 'Испрати',
  },
  objected: {
    key: 'closing.objected',
    en: 'Closing cancelled. The group is open again.',
    mk: 'Затворањето е откажано. Групата повторно е отворена.',
  },
  unlock: {
    key: 'closing.unlock',
    en: 'Unlock for changes',
    mk: 'Отклучи за промени',
  },
  unlocked: {
    key: 'closing.unlocked',
    en: 'Group unlocked.',
    mk: 'Групата е отклучена.',
  },
  badgeClosing: {
    key: 'closing.badgeClosing',
    en: 'Closing',
    mk: 'Се затвора',
  },
  badgeFinished: {
    key: 'closing.badgeFinished',
    en: 'Finished',
    mk: 'Завршена',
  },
  settingsFrozen: {
    key: 'closing.settingsFrozen',
    en: 'The group is being closed. Nothing can be changed.',
    mk: 'Групата се затвора. Ништо не може да се менува.',
  },
  addCancels: {
    key: 'closing.addCancels',
    en: 'The group is being closed. Saving this expense cancels the closing.',
    mk: 'Групата се затвора. Ако го зачуваш овој трошок, затворањето се откажува.',
  },
} satisfies Record<string, ClosingText>

export const finishedBarText: ClosingText = {
  key: 'groups.finishedReadOnly',
  en: 'Finished · read-only',
  mk: 'Завршена · само за читање',
}

export const closingActivityTexts = {
  closingStarted: {
    key: 'activity.closingStarted',
    en: '{{name}} started closing the group',
    mk: '{{name}} започна да ја затвора групата',
  },
  closingConfirmed: {
    key: 'activity.closingConfirmed',
    en: '{{name}} confirmed the closing',
    mk: '{{name}} го потврди затворањето',
  },
  closingObjected: {
    key: 'activity.closingObjected',
    en: '{{name}} objected to the closing',
    mk: '{{name}} приговори на затворањето',
  },
  closingObjectedWithReason: {
    key: 'activity.closingObjectedWithReason',
    en: '{{name}} objected to the closing: “{{reason}}”',
    mk: '{{name}} приговори на затворањето: „{{reason}}“',
  },
  closingCancelled: {
    key: 'activity.closingCancelled',
    en: '{{name}} cancelled the closing',
    mk: '{{name}} го откажа затворањето',
  },
  groupFinished: {
    key: 'activity.groupFinished',
    en: 'The group finished',
    mk: 'Групата заврши',
  },
  groupReopened: {
    key: 'activity.groupReopened',
    en: '{{name}} unlocked the group',
    mk: '{{name}} ја отклучи групата',
  },
} satisfies Record<string, ClosingText>

export function textOf(
  language: Language,
  text: ClosingText,
  params: Record<string, string> = {},
): string {
  return Object.entries(params).reduce(
    (result, [name, value]) => result.replaceAll(`{{${name}}}`, value),
    text[language],
  )
}
