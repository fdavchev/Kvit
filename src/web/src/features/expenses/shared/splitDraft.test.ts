import { describe, expect, it } from 'vitest'
import {
  buildSplitRequest,
  defaultSplitDraft,
  splitProgress,
  type SplitDraft,
} from './splitDraft'

const filipId = 'a1b2c3d4-0001-4aaa-8bbb-000000000001'
const anaId = 'a1b2c3d4-0002-4aaa-8bbb-000000000002'
const markoId = 'a1b2c3d4-0003-4aaa-8bbb-000000000003'
const grandmaId = 'a1b2c3d4-0004-4aaa-8bbb-000000000004'
const petarId = 'a1b2c3d4-0005-4aaa-8bbb-000000000005'

const fivePeople: readonly string[] = [filipId, anaId, markoId, grandmaId, petarId]
const threePeople: readonly string[] = [filipId, anaId, markoId]

const hotelMinor = 300000

function draftOf(
  splitType: SplitDraft['splitType'],
  typedValues: readonly string[],
  memberIds: readonly string[] = fivePeople,
  peopleOutOfTheSplit: readonly string[] = [],
): SplitDraft {
  return {
    splitType,
    people: memberIds.map((memberId, index) => ({
      memberId,
      isIn: !peopleOutOfTheSplit.includes(memberId),
      typedValue: typedValues[index] ?? '',
    })),
  }
}

describe('defaultSplitDraft', () => {
  it('is an Equal split with everybody in and nobody typed anything', () => {
    const draft = defaultSplitDraft(fivePeople)

    expect(draft).toEqual({
      splitType: 'Equal',
      people: fivePeople.map((memberId) => ({ memberId, isIn: true, typedValue: '' })),
    })
  })

  it('keeps the members in the order they were given, which is the joining order', () => {
    const draft = defaultSplitDraft([petarId, filipId, anaId])

    expect(draft.people.map((person) => person.memberId)).toEqual([petarId, filipId, anaId])
  })

  it('is already a split that can be saved', () => {
    expect(splitProgress(defaultSplitDraft(fivePeople), hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })
})

describe('splitProgress for an Equal split', () => {
  it('is ok when everybody is in and nobody has an extra', () => {
    expect(splitProgress(draftOf('Equal', []), hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('is ok for the hotel: 3,000 MKD, 5 people, Marko +600', () => {
    const draft = draftOf('Equal', ['', '', '600', '', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('is ok when the extras are exactly the total', () => {
    const draft = draftOf('Equal', ['', '', '3000', '', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('is not ok when one extra is above the total', () => {
    const draft = draftOf('Equal', ['', '', '3500', '', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })

  it('is not ok when the extras of several people together are above the total', () => {
    const draft = draftOf('Equal', ['1000', '1000', '1000', '1000', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })

  it('is ok with a single person in the split', () => {
    const draft = draftOf('Equal', [], fivePeople, [anaId, markoId, grandmaId, petarId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('is not ok when nobody is in the split', () => {
    const draft = draftOf('Equal', [], fivePeople, fivePeople)

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })

  it('ignores the extra of a person who is not in the split', () => {
    const draft = draftOf('Equal', ['', '9999', '', '', ''], fivePeople, [anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('counts the extras in cents for EUR: 2.50 is fine on 10.00, 10.01 is not', () => {
    expect(splitProgress(draftOf('Equal', ['', '', '2.50']), 1000, 'EUR')).toEqual({ kind: 'ok' })
    expect(splitProgress(draftOf('Equal', ['', '', '10.01']), 1000, 'EUR').kind).not.toBe('ok')
  })

  it('is not ok when an extra is not a number', () => {
    const draft = draftOf('Equal', ['', '', 'abc', '', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })
})

describe('splitProgress for an Exact split', () => {
  it('is ok when the typed amounts add up to the total: Marko 1,080 and four times 480 on 3,000 MKD', () => {
    const draft = draftOf('Exact', ['480', '480', '1080', '480', '480'])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('says how much is left to assign in deni when the amounts add up to less', () => {
    const draft = draftOf('Exact', ['480', '480', '1080', '480', '400'])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'amountLeft', minor: 8000 })
  })

  it('has the whole total left when nothing is typed yet', () => {
    expect(splitProgress(draftOf('Exact', []), hotelMinor, 'MKD')).toEqual({
      kind: 'amountLeft',
      minor: hotelMinor,
    })
  })

  it('says amountLeft with a negative number when the amounts add up to more than the total', () => {
    const draft = draftOf('Exact', ['1000', '1000', '1000', '500', ''])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'amountLeft', minor: -50000 })
  })

  it('ignores the amount of a person who is not in the split', () => {
    const draft = draftOf('Exact', ['1500', '9999', '1500'], threePeople, [anaId])

    expect(splitProgress(draft, 300000, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('counts cents for EUR', () => {
    expect(splitProgress(draftOf('Exact', ['4.50', '5.50'], [filipId, anaId]), 1000, 'EUR')).toEqual({
      kind: 'ok',
    })
    expect(splitProgress(draftOf('Exact', ['4.5', '5.4'], [filipId, anaId]), 1000, 'EUR')).toEqual({
      kind: 'amountLeft',
      minor: 10,
    })
  })

  it('is not ok when nobody is in the split', () => {
    const draft = draftOf('Exact', ['3000'], fivePeople, fivePeople)

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })

  it('is not ok when a typed amount is not a number', () => {
    const draft = draftOf('Exact', ['abc', '3000'], [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })
})

describe('splitProgress for a Percentage split', () => {
  it('is ok when four people have 25 % each', () => {
    const draft = draftOf('Percentage', ['25', '25', '25', '25'], [filipId, anaId, markoId, grandmaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('says how many hundredths of a percent are left: 25 + 25 + 25 + 12.5 leaves 12.5 %', () => {
    const draft = draftOf('Percentage', ['25', '25', '25', '12.5'], [filipId, anaId, markoId, grandmaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'percentLeft', hundredths: 1250 })
  })

  it('has 0.01 % left when three people have 33.33 % each', () => {
    const draft = draftOf('Percentage', ['33.33', '33.33', '33.33'], threePeople)

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'percentLeft', hundredths: 1 })
  })

  it('is ok when 33.34 + 33.33 + 33.33 add up to 100 %', () => {
    const draft = draftOf('Percentage', ['33.34', '33.33', '33.33'], threePeople)

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('has all 100 % left when nothing is typed yet', () => {
    expect(splitProgress(draftOf('Percentage', []), hotelMinor, 'MKD')).toEqual({
      kind: 'percentLeft',
      hundredths: 10000,
    })
  })

  it('is ok for one person with 100 %', () => {
    expect(splitProgress(draftOf('Percentage', ['100'], [filipId]), hotelMinor, 'MKD')).toEqual({
      kind: 'ok',
    })
  })

  it('says percentLeft with negative hundredths when the percentages add up to more than 100', () => {
    const draft = draftOf('Percentage', ['60', '60'], [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'percentLeft', hundredths: -2000 })
  })

  it('is not ok when a percentage has three decimals', () => {
    const draft = draftOf('Percentage', ['33.333', '66.667'], [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })

  it('ignores the percentage of a person who is not in the split', () => {
    const draft = draftOf('Percentage', ['50', '77', '50'], threePeople, [anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it.each(['MKD', 'EUR'] as const)('does not depend on the currency or the amount (%s)', (currency) => {
    const draft = draftOf('Percentage', ['25', '25', '25', '12.5'], [filipId, anaId, markoId, grandmaId])

    expect(splitProgress(draft, 12345, currency)).toEqual({ kind: 'percentLeft', hundredths: 1250 })
  })
})

describe('splitProgress for a Shares split', () => {
  it('is ok when everybody has one share', () => {
    expect(splitProgress(draftOf('Shares', ['1', '1', '1', '1', '1']), hotelMinor, 'MKD')).toEqual({
      kind: 'ok',
    })
  })

  it('is ok with different numbers of shares', () => {
    expect(splitProgress(draftOf('Shares', ['2', '1', '1'], threePeople), hotelMinor, 'MKD')).toEqual({
      kind: 'ok',
    })
  })

  it('says noShares when everybody has 0 shares', () => {
    expect(splitProgress(draftOf('Shares', ['0', '0', '0'], threePeople), hotelMinor, 'MKD')).toEqual({
      kind: 'noShares',
    })
  })

  it('says noShares when nothing is typed yet', () => {
    expect(splitProgress(draftOf('Shares', [], threePeople), hotelMinor, 'MKD')).toEqual({
      kind: 'noShares',
    })
  })

  it('is ok when one person has 0 shares and the others have some', () => {
    expect(splitProgress(draftOf('Shares', ['0', '1', '1'], threePeople), hotelMinor, 'MKD')).toEqual({
      kind: 'ok',
    })
  })

  it('says noShares when only people outside the split have shares', () => {
    const draft = draftOf('Shares', ['3', '3', '0'], threePeople, [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'noShares' })
  })

  it.each(['1.5', 'abc', '-1'])('is not ok when the shares are "%s"', (typedValue) => {
    const draft = draftOf('Shares', [typedValue, '1'], [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD').kind).not.toBe('ok')
  })
})

describe('buildSplitRequest for an Equal split', () => {
  it('lists everybody with an extra of 0 when nobody has an extra', () => {
    const request = buildSplitRequest(defaultSplitDraft(fivePeople), 'MKD')

    expect(request).toEqual(fivePeople.map((memberId) => ({ memberId, inputValue: 0 })))
  })

  it("sends Marko's +600 MKD as 60000 deni and 0 for the others", () => {
    const request = buildSplitRequest(draftOf('Equal', ['', '', '600', '', '']), 'MKD')

    expect(request).toEqual([
      { memberId: filipId, inputValue: 0 },
      { memberId: anaId, inputValue: 0 },
      { memberId: markoId, inputValue: 60000 },
      { memberId: grandmaId, inputValue: 0 },
      { memberId: petarId, inputValue: 0 },
    ])
  })

  it('sends an extra of 2.50 EUR as 250 cents', () => {
    const request = buildSplitRequest(draftOf('Equal', ['', '', '2.50'], threePeople), 'EUR')

    expect(request[2]).toEqual({ memberId: markoId, inputValue: 250 })
  })

  it('leaves out the people who are not in the split, even if they have an extra', () => {
    const request = buildSplitRequest(draftOf('Equal', ['', '700', '', ''], fivePeople, [anaId]), 'MKD')

    expect(request.map((share) => share.memberId)).toEqual([filipId, markoId, grandmaId, petarId])
    expect(request.every((share) => share.inputValue === 0)).toBe(true)
  })
})

describe('buildSplitRequest for the other split types', () => {
  it('sends Exact amounts in deni for MKD', () => {
    const request = buildSplitRequest(draftOf('Exact', ['480', '480', '1080', '480', '480']), 'MKD')

    expect(request.map((share) => share.inputValue)).toEqual([48000, 48000, 108000, 48000, 48000])
  })

  it('sends Exact amounts in cents for EUR', () => {
    const request = buildSplitRequest(draftOf('Exact', ['12.5', '7'], [filipId, anaId]), 'EUR')

    expect(request).toEqual([
      { memberId: filipId, inputValue: 1250 },
      { memberId: anaId, inputValue: 700 },
    ])
  })

  it('sends 0 for an Exact amount that was left empty', () => {
    const request = buildSplitRequest(draftOf('Exact', ['3000'], [filipId, anaId]), 'MKD')

    expect(request).toEqual([
      { memberId: filipId, inputValue: 300000 },
      { memberId: anaId, inputValue: 0 },
    ])
  })

  it('sends percentages in hundredths of a percent', () => {
    const request = buildSplitRequest(
      draftOf('Percentage', ['33.33', '25', '12.5', '100'], [filipId, anaId, markoId, grandmaId]),
      'MKD',
    )

    expect(request.map((share) => share.inputValue)).toEqual([3333, 2500, 1250, 10000])
  })

  it('sends whole numbers of shares and 0 for an empty one', () => {
    const request = buildSplitRequest(draftOf('Shares', ['2', '', '1'], threePeople), 'MKD')

    expect(request.map((share) => share.inputValue)).toEqual([2, 0, 1])
  })

  it.each(['Exact', 'Percentage', 'Shares'] as const)('leaves out the people who are not in a %s split', (splitType) => {
    const request = buildSplitRequest(draftOf(splitType, ['1', '1', '1'], threePeople, [anaId]), 'MKD')

    expect(request.map((share) => share.memberId)).toEqual([filipId, markoId])
  })

  it('keeps the order of the draft, which is the joining order', () => {
    const request = buildSplitRequest(draftOf('Shares', ['1', '1', '1'], [petarId, filipId, anaId]), 'MKD')

    expect(request.map((share) => share.memberId)).toEqual([petarId, filipId, anaId])
  })

  it.each(['Equal', 'Exact', 'Percentage', 'Shares'] as const)('sends a typed 0 as 0 in a %s split instead of treating it as a mistake', (splitType) => {
    const request = buildSplitRequest(draftOf(splitType, ['0', '0'], [filipId, anaId]), 'MKD')

    expect(request).toEqual([
      { memberId: filipId, inputValue: 0 },
      { memberId: anaId, inputValue: 0 },
    ])
  })

  it('counts a typed 0 as nothing in the progress of an Exact split', () => {
    const draft = draftOf('Exact', ['3000', '0'], [filipId, anaId])

    expect(splitProgress(draft, hotelMinor, 'MKD')).toEqual({ kind: 'ok' })
  })

  it('does not change the draft it was given', () => {
    const draft = draftOf('Exact', ['1500', '1500'], [filipId, anaId])
    const copy = structuredClone(draft)

    buildSplitRequest(draft, 'MKD')
    splitProgress(draft, hotelMinor, 'MKD')

    expect(draft).toEqual(copy)
  })

  it('stops with an error naming a typed value that is not a number, instead of sending a made-up 0', () => {
    expect(() => buildSplitRequest(draftOf('Exact', ['abc'], [filipId]), 'MKD')).toThrow('abc')
  })
})
