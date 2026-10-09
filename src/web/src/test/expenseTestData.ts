import type { Category } from '@/core/services/categories/categoriesService'
import type {
  DeletedExpenseRow,
  ExpenseDetail,
  ExpenseListRow,
} from '@/core/services/expenses/expensesService'
import { testGroupId } from './groupTestData'
import {
  anaMember,
  filipMember,
  grandmaMember,
  markoMember,
  petarMember,
} from './memberTestData'
import { testMe } from './testMe'

export const bojanUserId = '6e5d4c3b-2a19-4807-96f5-e4d3c2b1a098'

export const testNow = new Date('2026-10-06T10:00:00Z')
export const testToday = '2026-10-06'
export const testYesterday = '2026-10-05'

export const foodCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000001',
  key: 'food',
  emoji: '\u{1F37D}\u{FE0F}',
  color: 'orange',
  sortOrder: 1,
}

export const groceriesCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000002',
  key: 'groceries',
  emoji: '\u{1F6D2}',
  color: 'green',
  sortOrder: 2,
}

export const transportCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000003',
  key: 'transport',
  emoji: '\u{1F695}',
  color: 'yellow',
  sortOrder: 3,
}

export const accommodationCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000004',
  key: 'accommodation',
  emoji: '\u{1F3E8}',
  color: 'blue',
  sortOrder: 4,
}

export const funCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000005',
  key: 'fun',
  emoji: '\u{1F389}',
  color: 'pink',
  sortOrder: 5,
}

export const shoppingCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000006',
  key: 'shopping',
  emoji: '\u{1F6CD}\u{FE0F}',
  color: 'purple',
  sortOrder: 6,
}

export const billsCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000007',
  key: 'bills',
  emoji: '\u{1F9FE}',
  color: 'slate',
  sortOrder: 7,
}

export const healthCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000008',
  key: 'health',
  emoji: '\u{1F48A}',
  color: 'red',
  sortOrder: 8,
}

export const giftsCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000009',
  key: 'gifts',
  emoji: '\u{1F381}',
  color: 'teal',
  sortOrder: 9,
}

export const otherCategory: Category = {
  id: 'c0000000-0000-4000-8000-000000000010',
  key: 'other',
  emoji: '\u{1F4E6}',
  color: 'gray',
  sortOrder: 10,
}

export const testCategories: Category[] = [
  foodCategory,
  groceriesCategory,
  transportCategory,
  accommodationCategory,
  funCategory,
  shoppingCategory,
  billsCategory,
  healthCategory,
  giftsCategory,
  otherCategory,
]

export const categoryKeys: readonly string[] = testCategories.map((category) => category.key)

export const dinnerExpenseId = 'e0000000-0000-4000-8000-000000000001'
export const taxiExpenseId = 'e0000000-0000-4000-8000-000000000002'
export const groceriesExpenseId = 'e0000000-0000-4000-8000-000000000003'
export const hotelExpenseId = 'e0000000-0000-4000-8000-000000000004'
export const untitledExpenseId = 'e0000000-0000-4000-8000-000000000005'
export const lastYearExpenseId = 'e0000000-0000-4000-8000-000000000006'
export const museumExpenseId = 'e0000000-0000-4000-8000-000000000007'
export const pharmacyExpenseId = 'e0000000-0000-4000-8000-000000000008'

export const dinnerRow: ExpenseListRow = {
  id: dinnerExpenseId,
  title: 'Dinner',
  amountMinor: 240000,
  currency: 'MKD',
  expenseDate: '2026-10-06',
  categoryId: foodCategory.id,
  paidByMemberId: filipMember.id,
  paidByName: 'Filip',
  yourShareMinor: 60000,
  canEdit: true,
}

export const taxiRow: ExpenseListRow = {
  id: taxiExpenseId,
  title: 'Taxi',
  amountMinor: 60000,
  currency: 'MKD',
  expenseDate: '2026-10-05',
  categoryId: transportCategory.id,
  paidByMemberId: anaMember.id,
  paidByName: 'Ana',
  yourShareMinor: 15000,
  canEdit: false,
}

export const groceriesRow: ExpenseListRow = {
  id: groceriesExpenseId,
  title: 'Groceries',
  amountMinor: 4500,
  currency: 'EUR',
  expenseDate: '2026-10-05',
  categoryId: groceriesCategory.id,
  paidByMemberId: markoMember.id,
  paidByName: 'Marko',
  yourShareMinor: 1125,
  canEdit: true,
}

export const hotelRow: ExpenseListRow = {
  id: hotelExpenseId,
  title: 'Hotel',
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-03',
  categoryId: accommodationCategory.id,
  paidByMemberId: filipMember.id,
  paidByName: 'Filip',
  yourShareMinor: 48000,
  canEdit: true,
}

export const untitledRow: ExpenseListRow = {
  id: untitledExpenseId,
  title: null,
  amountMinor: 90000,
  currency: 'MKD',
  expenseDate: '2026-10-03',
  categoryId: otherCategory.id,
  paidByMemberId: grandmaMember.id,
  paidByName: 'Grandma',
  yourShareMinor: null,
  canEdit: false,
}

export const lastYearRow: ExpenseListRow = {
  id: lastYearExpenseId,
  title: 'Gift',
  amountMinor: 120000,
  currency: 'MKD',
  expenseDate: '2025-12-24',
  categoryId: giftsCategory.id,
  paidByMemberId: anaMember.id,
  paidByName: 'Ana',
  yourShareMinor: 40000,
  canEdit: false,
}

export const expenseRows: ExpenseListRow[] = [
  dinnerRow,
  taxiRow,
  groceriesRow,
  hotelRow,
  untitledRow,
  lastYearRow,
]

export const hotelDetail: ExpenseDetail = {
  id: hotelExpenseId,
  groupId: testGroupId,
  title: 'Hotel',
  note: 'Booked for the whole weekend',
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-03',
  categoryId: accommodationCategory.id,
  paidByMemberId: filipMember.id,
  paidByName: 'Filip',
  splitType: 'Equal',
  mkdPerEur: 61.561,
  rateDate: '2026-09-24',
  createdByUserId: bojanUserId,
  createdByName: 'Bojan',
  createdAt: '2026-10-03T08:00:00Z',
  updatedAt: '2026-10-04T09:00:00Z',
  canEdit: true,
  shares: [
    { memberId: filipMember.id, name: 'Filip', inputValue: 0, shareMinor: 48000 },
    { memberId: anaMember.id, name: 'Ana', inputValue: 0, shareMinor: 48000 },
    { memberId: markoMember.id, name: 'Marko', inputValue: 60000, shareMinor: 108000 },
    { memberId: grandmaMember.id, name: 'Grandma', inputValue: 0, shareMinor: 48000 },
    { memberId: petarMember.id, name: 'Petar', inputValue: 0, shareMinor: 48000 },
  ],
  history: [
    {
      type: 'ExpenseEdited',
      actorName: 'Ana',
      createdAt: '2026-10-04T09:00:00Z',
      changes: [{ field: 'amount', old: '280000', new: '300000' }],
    },
    {
      type: 'ExpenseAdded',
      actorName: 'Bojan',
      createdAt: '2026-10-03T08:00:00Z',
      changes: null,
    },
  ],
}

export const groceriesDetail: ExpenseDetail = {
  id: groceriesExpenseId,
  groupId: testGroupId,
  title: 'Groceries',
  note: null,
  amountMinor: 4500,
  currency: 'EUR',
  expenseDate: '2026-10-05',
  categoryId: groceriesCategory.id,
  paidByMemberId: markoMember.id,
  paidByName: 'Marko',
  splitType: 'Equal',
  mkdPerEur: 61.561,
  rateDate: '2026-09-24',
  createdByUserId: testMe.id,
  createdByName: 'Filip',
  createdAt: '2026-10-05T18:00:00Z',
  updatedAt: null,
  canEdit: true,
  shares: [
    { memberId: filipMember.id, name: 'Filip', inputValue: 0, shareMinor: 1500 },
    { memberId: anaMember.id, name: 'Ana', inputValue: 0, shareMinor: 1500 },
    { memberId: markoMember.id, name: 'Marko', inputValue: 0, shareMinor: 1500 },
  ],
  history: [
    {
      type: 'ExpenseAdded',
      actorName: 'Filip',
      createdAt: '2026-10-05T18:00:00Z',
      changes: null,
    },
  ],
}

export const historyDetail: ExpenseDetail = {
  ...hotelDetail,
  history: [
    {
      type: 'ExpenseRestored',
      actorName: 'Filip',
      createdAt: '2026-10-06T09:55:00Z',
      changes: null,
    },
    {
      type: 'ExpenseDeleted',
      actorName: 'Filip',
      createdAt: '2026-10-06T08:00:00Z',
      changes: null,
    },
    {
      type: 'ExpenseEdited',
      actorName: 'Ana',
      createdAt: '2026-10-04T10:00:00Z',
      changes: [{ field: 'amount', old: '280000', new: '300000' }],
    },
    {
      type: 'ExpenseEdited',
      actorName: 'Ana',
      createdAt: '2026-10-03T10:00:00Z',
      changes: [
        { field: 'title', old: '', new: 'Hotel' },
        { field: 'category', old: 'food', new: 'accommodation' },
      ],
    },
    {
      type: 'ExpenseEdited',
      actorName: 'Filip',
      createdAt: '2026-09-28T09:00:00Z',
      changes: [{ field: 'paidBy', old: 'Ana', new: 'Filip' }],
    },
    {
      type: 'ExpenseEdited',
      actorName: 'Filip',
      createdAt: '2026-09-27T09:00:00Z',
      changes: [
        { field: 'note', old: 'Booked', new: '' },
        { field: 'date', old: '2026-10-02', new: '2026-10-03' },
        { field: 'currency', old: 'EUR', new: 'MKD' },
        { field: 'split', old: 'Equal: Ana 600, Filip 600', new: 'Equal: Ana 100, Filip 100' },
      ],
    },
    {
      type: 'ExpenseAdded',
      actorName: 'Bojan',
      createdAt: '2026-09-20T09:00:00Z',
      changes: null,
    },
  ],
}

export const exactSplitDetail: ExpenseDetail = {
  ...hotelDetail,
  splitType: 'Exact',
  note: null,
  shares: [
    { memberId: filipMember.id, name: 'Filip', inputValue: 100000, shareMinor: 100000 },
    { memberId: anaMember.id, name: 'Ana', inputValue: 100000, shareMinor: 100000 },
    { memberId: markoMember.id, name: 'Marko', inputValue: 100000, shareMinor: 100000 },
  ],
}

export const museumDeletedRow: DeletedExpenseRow = {
  id: museumExpenseId,
  title: 'Museum tickets',
  amountMinor: 80000,
  currency: 'MKD',
  expenseDate: '2026-10-04',
  categoryId: funCategory.id,
  paidByName: 'Filip',
  deletedAt: '2026-10-05T12:00:00Z',
  restorableUntil: '2026-10-10T12:00:00Z',
  canEdit: true,
}

export const pharmacyDeletedRow: DeletedExpenseRow = {
  id: pharmacyExpenseId,
  title: 'Pharmacy',
  amountMinor: 35000,
  currency: 'MKD',
  expenseDate: '2026-10-03',
  categoryId: healthCategory.id,
  paidByName: 'Ana',
  deletedAt: '2026-10-04T12:00:00Z',
  restorableUntil: '2026-10-09T12:00:00Z',
  canEdit: false,
}

export const deletedRows: DeletedExpenseRow[] = [museumDeletedRow, pharmacyDeletedRow]

export function detailOf(changes: Partial<ExpenseDetail>): ExpenseDetail {
  return { ...hotelDetail, ...changes }
}
