import { describe, expect, it } from 'vitest'
import {
  buildSplitRequest,
  defaultSplitDraft,
  withPersonIn,
  withSplitType,
  withTypedValue,
  type SplitDraft,
} from '../shared/splitDraft'
import { buildOneBillRequest, type OneBillRequestInput } from './oneBillRequest'

const meId = 'person-me'
const markoId = 'person-marko'
const anaId = 'person-ana'
const bojanId = 'person-bojan'

const marko = { id: markoId, name: 'Marko' }
const ana = { id: anaId, name: 'Ana' }
const bojan = { id: bojanId, name: 'Bojan' }

function inputOf(changes: Partial<OneBillRequestInput>): OneBillRequestInput {
  return {
    clientRequestId: '7a9c1e3b-5d7f-4b9d-8f1a-3c5e7a9c1e3b',
    ownId: meId,
    names: [marko, ana],
    paidById: meId,
    splitDraft: defaultSplitDraft([meId, markoId, anaId]),
    title: '',
    groupName: 'Bill · 6 Oct',
    amountMinor: 180000,
    currency: 'MKD',
    expenseDate: '2026-10-06',
    categoryId: null,
    ...changes,
  }
}

function exactDraft(typedValues: [string, string][]): SplitDraft {
  let draft: SplitDraft = withSplitType(defaultSplitDraft([meId, markoId, anaId]), 'Exact')
  for (const [personId, typedValue] of typedValues) {
    draft = withTypedValue(draft, personId, typedValue)
  }
  return draft
}

describe('buildOneBillRequest', () => {
  it('keeps the request id, the amount, the currency, the date and the category as they are', () => {
    const request = buildOneBillRequest(
      inputOf({
        amountMinor: 4500,
        currency: 'EUR',
        expenseDate: '2026-12-12',
        categoryId: 'c0000000-0000-4000-8000-000000000001',
      }),
    )

    expect(request).toMatchObject({
      clientRequestId: '7a9c1e3b-5d7f-4b9d-8f1a-3c5e7a9c1e3b',
      amountMinor: 4500,
      currency: 'EUR',
      expenseDate: '2026-12-12',
      categoryId: 'c0000000-0000-4000-8000-000000000001',
    })
  })

  it('has exactly the 12 fields of the One bill request and never an emoji', () => {
    const request = buildOneBillRequest(inputOf({}))

    expect(Object.keys(request).sort()).toEqual(
      [
        'amountMinor',
        'categoryId',
        'clientRequestId',
        'currency',
        'expenseDate',
        'groupName',
        'names',
        'note',
        'paidByPersonIndex',
        'shares',
        'splitType',
        'title',
      ].sort(),
    )
  })

  it('sends the names in the order of the circles, without the caller', () => {
    const request = buildOneBillRequest(inputOf({ names: [marko, ana] }))

    expect(request.names).toEqual(['Marko', 'Ana'])
  })

  it('numbers the caller 0 and the names 1, 2, 3 in the order of the circles in the shares', () => {
    const request = buildOneBillRequest(
      inputOf({
        names: [marko, ana, bojan],
        splitDraft: defaultSplitDraft([meId, markoId, anaId, bojanId]),
      }),
    )

    expect(request.shares).toEqual([
      { personIndex: 0, inputValue: 0 },
      { personIndex: 1, inputValue: 0 },
      { personIndex: 2, inputValue: 0 },
      { personIndex: 3, inputValue: 0 },
    ])
  })

  it('points the payer at the caller with the index 0', () => {
    const request = buildOneBillRequest(inputOf({ paidById: meId }))

    expect(request.paidByPersonIndex).toBe(0)
  })

  it.each([
    [markoId, 1],
    [anaId, 2],
  ])('points the payer %s at the index %i', (paidById, index) => {
    const request = buildOneBillRequest(inputOf({ paidById }))

    expect(request.paidByPersonIndex).toBe(index)
  })

  it('sends a bill for one person with no names and the caller as the only share', () => {
    const request = buildOneBillRequest(
      inputOf({ names: [], splitDraft: defaultSplitDraft([meId]) }),
    )

    expect(request.names).toEqual([])
    expect(request.shares).toEqual([{ personIndex: 0, inputValue: 0 }])
    expect(request.paidByPersonIndex).toBe(0)
  })

  describe('a person who was removed from the circles', () => {
    it('is dropped from the shares of the split', () => {
      const request = buildOneBillRequest(inputOf({ names: [ana] }))

      expect(request.shares).toEqual([
        { personIndex: 0, inputValue: 0 },
        { personIndex: 1, inputValue: 0 },
      ])
    })

    it('does not leave a gap in the indexes: the people after the removed one move up', () => {
      const request = buildOneBillRequest(
        inputOf({
          names: [ana, bojan],
          paidById: bojanId,
          splitDraft: defaultSplitDraft([meId, markoId, anaId, bojanId]),
        }),
      )

      expect(request.names).toEqual(['Ana', 'Bojan'])
      expect(request.paidByPersonIndex).toBe(2)
      expect(request.shares.map((share) => share.personIndex)).toEqual([0, 1, 2])
    })

    it('puts the payer back to the caller when the payer was removed', () => {
      const request = buildOneBillRequest(inputOf({ names: [ana], paidById: markoId }))

      expect(request.paidByPersonIndex).toBe(0)
    })

    it('keeps the typed values of the people who stay, in an Exact split', () => {
      const request = buildOneBillRequest(
        inputOf({
          names: [ana],
          splitDraft: exactDraft([
            [meId, '600'],
            [markoId, '600'],
            [anaId, '600'],
          ]),
          amountMinor: 120000,
        }),
      )

      expect(request.splitType).toBe('Exact')
      expect(request.shares).toEqual([
        { personIndex: 0, inputValue: 60000 },
        { personIndex: 1, inputValue: 60000 },
      ])
    })
  })

  describe('the split', () => {
    it('sends only the people who are in the split, by their index', () => {
      const draft = withPersonIn(defaultSplitDraft([meId, markoId, anaId]), markoId, false)

      const request = buildOneBillRequest(inputOf({ splitDraft: draft }))

      expect(request.shares).toEqual([
        { personIndex: 0, inputValue: 0 },
        { personIndex: 2, inputValue: 0 },
      ])
    })

    it('gives the same values as buildSplitRequest for an Exact split in denars, only with indexes for people', () => {
      const draft = exactDraft([
        [meId, '600'],
        [markoId, '300'],
        [anaId, '900'],
      ])
      const wanted = buildSplitRequest(draft, 'MKD')

      const request = buildOneBillRequest(inputOf({ splitDraft: draft, amountMinor: 180000 }))

      expect(request.shares.map((share) => share.inputValue)).toEqual(
        wanted.map((share) => share.inputValue),
      )
      expect(request.shares).toEqual([
        { personIndex: 0, inputValue: 60000 },
        { personIndex: 1, inputValue: 30000 },
        { personIndex: 2, inputValue: 90000 },
      ])
    })

    it('counts an Exact split in cents for EUR', () => {
      const request = buildOneBillRequest(
        inputOf({
          currency: 'EUR',
          amountMinor: 3000,
          splitDraft: exactDraft([
            [meId, '10.50'],
            [markoId, '9,5'],
            [anaId, '10'],
          ]),
        }),
      )

      expect(request.shares.map((share) => share.inputValue)).toEqual([1050, 950, 1000])
    })

    it('counts a percentage split in hundredths of a percent', () => {
      let draft = withSplitType(defaultSplitDraft([meId, markoId, anaId]), 'Percentage')
      draft = withTypedValue(draft, meId, '50')
      draft = withTypedValue(draft, markoId, '25')
      draft = withTypedValue(draft, anaId, '25')

      const request = buildOneBillRequest(inputOf({ splitDraft: draft }))

      expect(request.splitType).toBe('Percentage')
      expect(request.shares.map((share) => share.inputValue)).toEqual([5000, 2500, 2500])
    })

    it('sends whole shares for a Shares split', () => {
      let draft = withSplitType(defaultSplitDraft([meId, markoId, anaId]), 'Shares')
      draft = withTypedValue(draft, markoId, '2')

      const request = buildOneBillRequest(inputOf({ splitDraft: draft }))

      expect(request.splitType).toBe('Shares')
      expect(request.shares.map((share) => share.inputValue)).toEqual([1, 2, 1])
    })

    it('sends the extra of an Equal split in minor units', () => {
      const draft = withTypedValue(defaultSplitDraft([meId, markoId, anaId]), markoId, '300')

      const request = buildOneBillRequest(inputOf({ splitDraft: draft }))

      expect(request.splitType).toBe('Equal')
      expect(request.shares.map((share) => share.inputValue)).toEqual([0, 30000, 0])
    })

    it('stops with a clear error naming the value when a typed value is not a number', () => {
      const draft = exactDraft([[markoId, 'abc']])

      expect(() => buildOneBillRequest(inputOf({ splitDraft: draft }))).toThrow('abc')
    })
  })

  describe('the title', () => {
    it.each([
      ['Dinner', 'Dinner'],
      ['  Dinner at Mario  ', 'Dinner at Mario'],
    ])('sends the title %j as %j', (typed, sent) => {
      const request = buildOneBillRequest(inputOf({ title: typed }))

      expect(request.title).toBe(sent)
    })

    it.each([[''], ['   ']])('sends null for the empty title %j', (typed) => {
      const request = buildOneBillRequest(inputOf({ title: typed }))

      expect(request.title).toBeNull()
    })

    it.each([[''], ['   ']])('sends the given group name next to the empty title %j, and the title stays null', (typed) => {
      const request = buildOneBillRequest(inputOf({ title: typed, groupName: 'Сметка · 7 окт' }))

      expect(request.groupName).toBe('Сметка · 7 окт')
      expect(request.title).toBeNull()
    })

    it.each([['Dinner'], ['  Dinner at Mario  ']])('sends a null group name when the title %j is typed, so the title names the group', (typed) => {
      const request = buildOneBillRequest(inputOf({ title: typed, groupName: 'Bill · 6 Oct' }))

      expect(request.groupName).toBeNull()
      expect(request.title).not.toBeNull()
    })

    it('sends no note', () => {
      const request = buildOneBillRequest(inputOf({}))

      expect(request.note).toBeNull()
    })
  })

  it('does not change the draft or the list of names it is given', () => {
    const draft = withPersonIn(defaultSplitDraft([meId, markoId, anaId]), anaId, false)
    const names = [marko, ana]
    const draftBefore = structuredClone(draft)
    const namesBefore = structuredClone(names)

    buildOneBillRequest(inputOf({ names: [marko], splitDraft: draft, paidById: anaId }))
    buildOneBillRequest(inputOf({ names, splitDraft: draft }))

    expect(draft).toEqual(draftBefore)
    expect(names).toEqual(namesBefore)
  })
})
