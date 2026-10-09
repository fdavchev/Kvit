import type { OneBillInput } from '@/core/services/expenses/expensesService'
import type { Currency } from '@/shared/utils/formatMoney'
import { textOrNull } from '../shared/expenseTitle'
import { buildSplitRequest, type SplitDraft } from '../shared/splitDraft'

export interface BillName {
  id: string
  name: string
}

export interface OneBillRequestInput {
  clientRequestId: string
  ownId: string
  names: readonly BillName[]
  paidById: string
  splitDraft: SplitDraft
  title: string
  groupName: string
  amountMinor: number
  currency: Currency
  expenseDate: string
  categoryId: string | null
}

const ownIndex = 0

export function buildOneBillRequest(input: OneBillRequestInput): OneBillInput {
  const personIds: string[] = [input.ownId, ...input.names.map((billName) => billName.id)]
  const draftOfThePeopleLeft: SplitDraft = {
    ...input.splitDraft,
    people: input.splitDraft.people.filter((person) => personIds.includes(person.memberId)),
  }
  const payerIndex = personIds.indexOf(input.paidById)
  const title = textOrNull(input.title)
  return {
    clientRequestId: input.clientRequestId,
    title,
    groupName: title === null ? input.groupName : null,
    names: input.names.map((billName) => billName.name),
    note: null,
    amountMinor: input.amountMinor,
    currency: input.currency,
    expenseDate: input.expenseDate,
    categoryId: input.categoryId,
    paidByPersonIndex: payerIndex === -1 ? ownIndex : payerIndex,
    splitType: draftOfThePeopleLeft.splitType,
    shares: buildSplitRequest(draftOfThePeopleLeft, input.currency).map((share) => ({
      personIndex: personIds.indexOf(share.memberId),
      inputValue: share.inputValue,
    })),
  }
}
