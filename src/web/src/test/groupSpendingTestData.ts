import {
  accommodationCategory,
  foodCategory,
  funCategory,
  groceriesCategory,
  transportCategory,
} from './expenseTestData'
import { groupPath } from './expenseTestHelpers'

export const spendingPath = `${groupPath}/spending`

export const unknownCategoryId = 'c0000000-0000-4000-8000-0000000000ff'

export const bigDenarAmountMinor = 99999999990000

export const planFields = [
  'amountMinor',
  'currency',
  'spentMinor',
  'perPersonMinor',
  'overMinor',
  'includedOtherCurrencyMinor',
] as const

export const noSpendingYet = { currencies: [], plan: null }

export const mkdOnlySpending = {
  currencies: [
    {
      currency: 'MKD',
      totalMinor: 135000,
      categories: [
        { categoryId: foodCategory.id, totalMinor: 80000 },
        { categoryId: transportCategory.id, totalMinor: 30000 },
        { categoryId: null, totalMinor: 25000 },
      ],
    },
  ],
  plan: null,
}

export const eurOnlySpending = {
  currencies: [
    {
      currency: 'EUR',
      totalMinor: 8250,
      categories: [
        { categoryId: groceriesCategory.id, totalMinor: 5000 },
        { categoryId: funCategory.id, totalMinor: 3250 },
      ],
    },
  ],
  plan: null,
}

export const mkdAndEurSpending = {
  currencies: [...mkdOnlySpending.currencies, ...eurOnlySpending.currencies],
  plan: null,
}

export const spendingWithPlan = {
  ...mkdOnlySpending,
  plan: {
    amountMinor: 200000,
    currency: 'MKD',
    spentMinor: 135000,
    perPersonMinor: 200000,
    overMinor: 0,
    includedOtherCurrencyMinor: 0,
  },
}

export const bigDenarSpending = {
  currencies: [
    {
      currency: 'MKD',
      totalMinor: bigDenarAmountMinor,
      categories: [{ categoryId: accommodationCategory.id, totalMinor: bigDenarAmountMinor }],
    },
  ],
  plan: null,
}

export const onlyUncategorisedSpending = {
  currencies: [{ currency: 'MKD', totalMinor: 40000, categories: [{ categoryId: null, totalMinor: 40000 }] }],
  plan: null,
}

export const unknownCategorySpending = {
  currencies: [
    {
      currency: 'MKD',
      totalMinor: 40000,
      categories: [{ categoryId: unknownCategoryId, totalMinor: 40000 }],
    },
  ],
  plan: null,
}

export const laterAnswerSpending = {
  currencies: [
    {
      currency: 'MKD',
      totalMinor: 220000,
      categories: [
        { categoryId: foodCategory.id, totalMinor: 150000 },
        { categoryId: transportCategory.id, totalMinor: 70000 },
      ],
    },
  ],
  plan: null,
}
