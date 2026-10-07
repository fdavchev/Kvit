import { describe, expect, it } from 'vitest'
import {
  buildSplitRequest,
  defaultSplitDraft,
  splitProgress,
  withAutoFillRefreshed,
  withPersonIn,
  withSplitType,
  withTypedValueAutoFilled,
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
      isTyped: (typedValues[index] ?? '') !== '',
    })),
  }
}

describe('defaultSplitDraft', () => {
  it('is an Equal split with everybody in and nobody typed anything', () => {
    const draft = defaultSplitDraft(fivePeople)

    expect(draft).toEqual({
      splitType: 'Equal',
      people: fivePeople.map((memberId) => ({
        memberId,
        isIn: true,
        typedValue: '',
        isTyped: false,
      })),
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

const twoPeople: readonly string[] = [filipId, anaId]
const eighteenHundredMkd = 180000
const eighteenEur = 1800

function emptyDraftOf(
  splitType: SplitDraft['splitType'],
  memberIds: readonly string[] = twoPeople,
): SplitDraft {
  return withSplitType(defaultSplitDraft(memberIds), splitType)
}

function typedValuesOf(draft: SplitDraft): string[] {
  return draft.people.map((person) => person.typedValue)
}

function typedFlagsOf(draft: SplitDraft): boolean[] {
  return draft.people.map((person) => person.isTyped)
}

function exactDraftWithFilipTyped(typedValue: string): SplitDraft {
  return withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, typedValue, eighteenHundredMkd, 'MKD')
}

describe('withSplitType', () => {
  it('clears every typed value and every typed flag when the type changes', () => {
    const typed = exactDraftWithFilipTyped('700')

    const draft = withSplitType(typed, 'Percentage')

    expect(draft.splitType).toBe('Percentage')
    expect(typedValuesOf(draft)).toEqual(['', ''])
    expect(typedFlagsOf(draft)).toEqual([false, false])
  })

  it('clears the typed values and flags also when the type stays the same, which is what a currency switch does', () => {
    const draft = withSplitType(exactDraftWithFilipTyped('700'), 'Exact')

    expect(typedValuesOf(draft)).toEqual(['', ''])
    expect(typedFlagsOf(draft)).toEqual([false, false])
  })

  it('starts every person who is in with 1 share on the Shares tab and leaves a person who is out empty', () => {
    const withMarkoOut = withPersonIn(defaultSplitDraft(threePeople), markoId, false)

    const draft = withSplitType(withMarkoOut, 'Shares')

    expect(typedValuesOf(draft)).toEqual(['1', '1', ''])
    expect(typedFlagsOf(draft)).toEqual([false, false, false])
  })

  it('keeps the people, their order and who is in', () => {
    const withMarkoOut = withPersonIn(defaultSplitDraft(threePeople), markoId, false)

    const draft = withSplitType(withMarkoOut, 'Exact')

    expect(draft.people.map((person) => [person.memberId, person.isIn])).toEqual([
      [filipId, true],
      [anaId, true],
      [markoId, false],
    ])
  })

  it('lets the auto-fill start again from nothing after a tab was switched', () => {
    const afterSwitch = withSplitType(exactDraftWithFilipTyped('700'), 'Percentage')

    const draft = withTypedValueAutoFilled(afterSwitch, anaId, '30', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['70', '30'])
  })
})

describe('withTypedValueAutoFilled on the Exact tab with two people', () => {
  it.each([
    ['700', '1100'],
    ['0', '1800'],
    ['1800', '0'],
    ['1', '1799'],
    ['1799', '1'],
  ])('fills Ana with the rest of 1,800 MKD when Filip types %s: %s', (typed, rest) => {
    const draft = exactDraftWithFilipTyped(typed)

    expect(typedValuesOf(draft)).toEqual([typed, rest])
  })

  it.each([
    ['7.25', '10.75'],
    ['7.5', '10.50'],
    ['7,25', '10.75'],
    ['0.01', '17.99'],
    ['18', '0'],
    ['0', '18'],
  ])('fills Ana with the rest of 18.00 EUR in cents when Filip types %s: %s', (typed, rest) => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, typed, eighteenEur, 'EUR')

    expect(typedValuesOf(draft)).toEqual([typed, rest])
  })

  it('fills Filip with the rest when Ana is the one who types', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), anaId, '500', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['1300', '500'])
  })

  it('keeps the typed text exactly as typed and marks only that person as typed', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, '7.50', eighteenEur, 'EUR')

    expect(draft.people[0]).toMatchObject({ memberId: filipId, typedValue: '7.50', isTyped: true })
    expect(draft.people[1]).toMatchObject({ memberId: anaId, isTyped: false })
  })

  it('updates the filled amount every time the typed amount changes', () => {
    const first = exactDraftWithFilipTyped('700')

    const second = withTypedValueAutoFilled(first, filipId, '800', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(second)).toEqual(['800', '1000'])
  })

  it('counts the filled amount as typed: the split adds up and is sent with both amounts', () => {
    const draft = exactDraftWithFilipTyped('700')

    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'ok' })
    expect(buildSplitRequest(draft, 'MKD')).toEqual([
      { memberId: filipId, inputValue: 70000 },
      { memberId: anaId, inputValue: 110000 },
    ])
  })

  it('counts the filled amount as typed in cents for EUR', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, '7.25', eighteenEur, 'EUR')

    expect(splitProgress(draft, eighteenEur, 'EUR')).toEqual({ kind: 'ok' })
    expect(buildSplitRequest(draft, 'EUR').map((share) => share.inputValue)).toEqual([725, 1075])
  })

  it('leaves the other person empty when the typed amount is above the total, so the counter shows the minus', () => {
    const draft = exactDraftWithFilipTyped('2000')

    expect(typedValuesOf(draft)).toEqual(['2000', ''])
    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'amountLeft', minor: -20000 })
  })

  it('empties a filled amount again when a later typed amount goes above the total', () => {
    const draft = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), filipId, '2000', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['2000', ''])
  })

  it('leaves the other person empty while there is no amount yet', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, '700', 0, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['700', ''])
  })

  it('empties the filled amount and the typed flag when the typed box is cleared', () => {
    const draft = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), filipId, '', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['', ''])
    expect(typedFlagsOf(draft)).toEqual([false, false])
  })

  it('keeps a text that is not a number in the box without throwing, and the split is not a number', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, 'abc', eighteenHundredMkd, 'MKD')

    expect(draft.people[0]).toMatchObject({ typedValue: 'abc', isTyped: true })
    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'notANumber' })
  })

  it('does not change the draft it was given', () => {
    const draft = emptyDraftOf('Exact')
    const copy = structuredClone(draft)

    withTypedValueAutoFilled(draft, filipId, '700', eighteenHundredMkd, 'MKD')

    expect(draft).toEqual(copy)
  })
})

describe('withTypedValueAutoFilled when the other person has typed too', () => {
  it('keeps the typed amount of the first person and shows the gap when the other person types an amount', () => {
    const draft = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), anaId, '500', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['700', '500'])
    expect(typedFlagsOf(draft)).toEqual([true, true])
    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'amountLeft', minor: 60000 })
  })

  it('does not overwrite the second box when the first box is typed in again afterwards', () => {
    const bothTyped = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), anaId, '500', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(bothTyped, filipId, '900', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['900', '500'])
  })

  it('counts a typed 0 as typed: the other box typed afterwards does not overwrite it', () => {
    const zeroTyped = exactDraftWithFilipTyped('0')

    const draft = withTypedValueAutoFilled(zeroTyped, anaId, '500', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(zeroTyped)).toEqual(['0', '1800'])
    expect(typedValuesOf(draft)).toEqual(['0', '500'])
    expect(typedFlagsOf(draft)).toEqual([true, true])
  })

  it('does not overwrite a box that was typed first when the other box is typed second', () => {
    const anaFirst = withTypedValueAutoFilled(emptyDraftOf('Exact'), anaId, '500', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(anaFirst, filipId, '200', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['200', '500'])
  })

  it('leaves a typed box empty when it is cleared and does not fill it from the other typed box', () => {
    const bothTyped = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), anaId, '500', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(bothTyped, filipId, '', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['', '500'])
    expect(typedFlagsOf(draft)).toEqual([false, true])
  })

  it('fills the other box again after a typed box was cleared and the first box is typed in again', () => {
    const bothTyped = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), anaId, '500', eighteenHundredMkd, 'MKD')
    const anaCleared = withTypedValueAutoFilled(bothTyped, anaId, '', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(anaCleared, filipId, '800', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(anaCleared)).toEqual(['700', ''])
    expect(typedValuesOf(draft)).toEqual(['800', '1000'])
  })
})

describe('withTypedValueAutoFilled on the % tab with two people', () => {
  it.each([
    ['30', '70'],
    ['33.33', '66.67'],
    ['12.5', '87.5'],
    ['0', '100'],
    ['100', '0'],
    ['0.01', '99.99'],
    ['99.99', '0.01'],
  ])('fills Ana with the rest of 100 percent when Filip types %s: %s', (typed, rest) => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, typed, eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual([typed, rest])
  })

  it('fills Filip when Ana is the one who types', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Percentage'), anaId, '25', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['75', '25'])
  })

  it.each(['MKD', 'EUR'] as const)('does not depend on the amount or the currency (%s)', (currency) => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '30', 12345, currency)

    expect(typedValuesOf(draft)).toEqual(['30', '70'])
  })

  it('counts the filled percentage as typed: the split adds up and is sent in hundredths of a percent', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '33.33', eighteenHundredMkd, 'MKD')

    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'ok' })
    expect(buildSplitRequest(draft, 'MKD').map((share) => share.inputValue)).toEqual([3333, 6667])
  })

  it('leaves the other person empty when the typed percentage is above 100, so the counter shows the minus', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '120', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['120', ''])
    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'percentLeft', hundredths: -2000 })
  })

  it('keeps the typed percentage of the first person when the other person types one', () => {
    const first = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '30', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(first, anaId, '50', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['30', '50'])
    expect(splitProgress(draft, eighteenHundredMkd, 'MKD')).toEqual({ kind: 'percentLeft', hundredths: 2000 })
  })

  it('empties the filled percentage when the typed box is cleared', () => {
    const first = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '30', eighteenHundredMkd, 'MKD')

    const draft = withTypedValueAutoFilled(first, filipId, '', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['', ''])
  })
})

describe('withTypedValueAutoFilled when nothing may be filled', () => {
  it.each(['Exact', 'Percentage'] as const)('fills nobody with three people on the %s tab', (splitType) => {
    const draft = withTypedValueAutoFilled(
      emptyDraftOf(splitType, threePeople),
      filipId,
      '480',
      eighteenHundredMkd,
      'MKD',
    )

    expect(typedValuesOf(draft)).toEqual(['480', '', ''])
  })

  it('fills nobody with five people on the Exact tab', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Exact', fivePeople), filipId, '480', hotelMinor, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['480', '', '', '', ''])
  })

  it.each(['Exact', 'Percentage'] as const)('fills nobody with a single person in on the %s tab', (splitType) => {
    const onlyFilip = withPersonIn(withPersonIn(emptyDraftOf(splitType, threePeople), anaId, false), markoId, false)

    const draft = withTypedValueAutoFilled(onlyFilip, filipId, '480', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['480', '', ''])
  })

  it.each([
    ['Exact', '700', '1100'],
    ['Percentage', '30', '70'],
  ] as const)('fills the other person who is in and not the person who is out when two of three are in on the %s tab', (splitType, typed, rest) => {
    const markoOut = withPersonIn(emptyDraftOf(splitType, threePeople), markoId, false)

    const draft = withTypedValueAutoFilled(markoOut, filipId, typed, eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual([typed, rest, ''])
  })

  it('fills nobody with two people on the Equal tab, where a typed value is an extra', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Equal'), filipId, '300', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['300', ''])
  })

  it('fills nobody with two people on the Shares tab', () => {
    const draft = withTypedValueAutoFilled(emptyDraftOf('Shares'), filipId, '3', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['3', '1'])
  })

  it('fills nobody once a third person has joined the split, and keeps the amounts that are already there', () => {
    const twoFilled = exactDraftWithFilipTyped('700')
    const threeIn: SplitDraft = {
      ...twoFilled,
      people: [...twoFilled.people, { memberId: markoId, isIn: true, typedValue: '', isTyped: false }],
    }

    const draft = withTypedValueAutoFilled(threeIn, filipId, '800', eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['800', '1100', ''])
  })
})

describe('withAutoFillRefreshed', () => {
  it('re-fills the box nobody typed in when the total changes', () => {
    const draft = withAutoFillRefreshed(exactDraftWithFilipTyped('700'), 200000, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['700', '1300'])
  })

  it('re-fills Filip when Ana was the one who typed', () => {
    const anaTyped = withTypedValueAutoFilled(emptyDraftOf('Exact'), anaId, '500', eighteenHundredMkd, 'MKD')

    const draft = withAutoFillRefreshed(anaTyped, 200000, 'MKD')

    expect(typedValuesOf(draft)).toEqual(['1500', '500'])
  })

  it('re-fills in cents for EUR', () => {
    const typed = withTypedValueAutoFilled(emptyDraftOf('Exact'), filipId, '7.25', eighteenEur, 'EUR')

    const draft = withAutoFillRefreshed(typed, 2000, 'EUR')

    expect(typedValuesOf(draft)).toEqual(['7.25', '12.75'])
  })

  it('keeps both amounts when both boxes were typed in', () => {
    const bothTyped = withTypedValueAutoFilled(exactDraftWithFilipTyped('700'), anaId, '500', eighteenHundredMkd, 'MKD')

    const draft = withAutoFillRefreshed(bothTyped, 200000, 'MKD')

    expect(draft).toEqual(bothTyped)
  })

  it('changes nothing when nobody typed anything', () => {
    const empty = emptyDraftOf('Exact')

    expect(withAutoFillRefreshed(empty, 200000, 'MKD')).toEqual(empty)
  })

  it('empties the filled box when the total drops below the typed amount and fills it again when the total rises', () => {
    const typed = exactDraftWithFilipTyped('700')

    const dropped = withAutoFillRefreshed(typed, 50000, 'MKD')
    const raised = withAutoFillRefreshed(dropped, eighteenHundredMkd, 'MKD')

    expect(typedValuesOf(dropped)).toEqual(['700', ''])
    expect(typedValuesOf(raised)).toEqual(['700', '1100'])
  })

  it('changes nothing on the % tab, where the total does not matter', () => {
    const typed = withTypedValueAutoFilled(emptyDraftOf('Percentage'), filipId, '30', eighteenHundredMkd, 'MKD')

    expect(withAutoFillRefreshed(typed, 200000, 'MKD')).toEqual(typed)
  })

  it('changes nothing with three people', () => {
    const typed = withTypedValueAutoFilled(emptyDraftOf('Exact', threePeople), filipId, '480', eighteenHundredMkd, 'MKD')

    expect(withAutoFillRefreshed(typed, 200000, 'MKD')).toEqual(typed)
  })

  it.each(['Equal', 'Shares'] as const)('changes nothing on the %s tab', (splitType) => {
    const typed = withTypedValueAutoFilled(emptyDraftOf(splitType), filipId, '3', eighteenHundredMkd, 'MKD')

    expect(withAutoFillRefreshed(typed, 200000, 'MKD')).toEqual(typed)
  })

  it('does not change the draft it was given', () => {
    const draft = exactDraftWithFilipTyped('700')
    const copy = structuredClone(draft)

    withAutoFillRefreshed(draft, 200000, 'MKD')

    expect(draft).toEqual(copy)
  })
})
