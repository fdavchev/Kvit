import type {
  BuiltInPersonalCategory,
  CustomPersonalCategory,
  PersonalCategory,
} from '@/core/services/personalCategories/personalCategoriesService'

export const personalFoodCategory: BuiltInPersonalCategory = {
  id: 'c0000000-0000-4000-8000-000000000001',
  key: 'food',
  name: null,
  emoji: '\u{1F37D}\u{FE0F}',
  color: 'orange',
  sortOrder: 1,
  isCustom: false,
  archived: false,
}

export const personalGroceriesCategory: BuiltInPersonalCategory = {
  id: 'c0000000-0000-4000-8000-000000000002',
  key: 'groceries',
  name: null,
  emoji: '\u{1F6D2}',
  color: 'green',
  sortOrder: 2,
  isCustom: false,
  archived: false,
}

export const coffeeCategory: CustomPersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000001',
  key: null,
  name: 'Coffee',
  emoji: '\u{2615}',
  color: 'teal',
  sortOrder: 100,
  isCustom: true,
  archived: false,
}

export const petCategory: CustomPersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000002',
  key: null,
  name: 'Dog',
  emoji: '\u{1F436}',
  color: 'purple',
  sortOrder: 101,
  isCustom: true,
  archived: false,
}

export const archivedGymCategory: CustomPersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000003',
  key: null,
  name: 'Gym',
  emoji: '\u{1F3CB}\u{FE0F}',
  color: 'red',
  sortOrder: 102,
  isCustom: true,
  archived: true,
}

export const createdTeaCategory: CustomPersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000009',
  key: null,
  name: 'Tea',
  emoji: '\u{1F375}',
  color: 'green',
  sortOrder: 103,
  isCustom: true,
  archived: false,
}

export const activePersonalCategories: PersonalCategory[] = [
  personalFoodCategory,
  personalGroceriesCategory,
  coffeeCategory,
  petCategory,
]

export const personalCategoriesWithArchived: PersonalCategory[] = [
  ...activePersonalCategories,
  archivedGymCategory,
]

export const builtInPersonalCategoriesOnly: PersonalCategory[] = [
  personalFoodCategory,
  personalGroceriesCategory,
]
