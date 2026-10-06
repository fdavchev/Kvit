import type { TFunction } from 'i18next'
import type { GroupMember } from '@/core/services/groups/membersService'

export interface ExpensePerson {
  memberId: string
  name: string
  pictureUrl: string | null
  colorIndex: number
  isYou: boolean
  isCurrentMember: boolean
}

export interface FormerPerson {
  memberId: string
  name: string
}

const neutralColorIndex = 9

export function expensePeople(
  members: readonly GroupMember[],
  formerPeople: readonly FormerPerson[] = [],
): ExpensePerson[] {
  const current: ExpensePerson[] = members.map((member, index) => ({
    memberId: member.id,
    name: member.displayName,
    pictureUrl: member.pictureUrl,
    colorIndex: index,
    isYou: member.isYou,
    isCurrentMember: true,
  }))
  const former: ExpensePerson[] = []
  for (const person of formerPeople) {
    const isListed: boolean = [...current, ...former].some(
      (listed) => listed.memberId === person.memberId,
    )
    if (!isListed) {
      former.push({
        memberId: person.memberId,
        name: person.name,
        pictureUrl: null,
        colorIndex: neutralColorIndex,
        isYou: false,
        isCurrentMember: false,
      })
    }
  }
  return [...current, ...former]
}

export function personOf(people: readonly ExpensePerson[], memberId: string): ExpensePerson {
  const person = people.find((candidate) => candidate.memberId === memberId)
  if (person === undefined) {
    throw new Error(`Expected the person ${memberId} to be among the people of the expense, found none`)
  }
  return person
}

export function personLabel(person: ExpensePerson, t: TFunction): string {
  return person.isYou ? `${person.name} (${t('expense.me')})` : person.name
}
