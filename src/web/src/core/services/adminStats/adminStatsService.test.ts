import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { allTimeStats, novemberStats, statsWith } from '@/test/adminStatsTestData'
import { getAdminStats } from './adminStatsService'

const novemberRequest = { from: '2026-11-01', to: '2026-11-04', by: 'day' } as const

type Section = 'allTime' | 'signUps' | 'activePeople' | 'joinedViaInvite' | 'groupsCreated' | 'expensesAdded' | 'paymentsConfirmed'

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function bodyWith(fields: Record<string, unknown>): Record<string, unknown> {
  return { ...novemberStats, ...fields }
}

function bodyWithSection(section: Section, fields: Record<string, unknown>): Record<string, unknown> {
  return bodyWith({ [section]: { ...novemberStats[section], ...fields } })
}

function bodyWithoutSectionField(section: Section, field: string): Record<string, unknown> {
  return bodyWith({ [section]: withoutField(novemberStats[section], field) })
}

describe('getAdminStats', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/admin/stats with from, to and by for the range', async () => {
    const fetchMock = stubFetch(Response.json(novemberStats))

    await getAdminStats(novemberRequest)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/admin/stats?from=2026-11-01&to=2026-11-04&by=day')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('sends only the grouping when no range is given', async () => {
    const fetchMock = stubFetch(Response.json(allTimeStats))

    await getAdminStats({ by: 'month' })

    expect(sentRequest(fetchMock).url).toBe('/api/admin/stats?by=month')
  })

  it.each(['day', 'week', 'month', 'year'] as const)('sends the grouping %s as it is', async (by) => {
    const fetchMock = stubFetch(Response.json(statsWith({ by })))

    await getAdminStats({ from: '2026-01-01', to: '2026-11-04', by })

    expect(sentRequest(fetchMock).url).toBe(`/api/admin/stats?from=2026-01-01&to=2026-11-04&by=${by}`)
  })

  it('returns every field of the answer, with the buckets and the counts in the order of the answer', async () => {
    stubFetch(Response.json(novemberStats))

    expect(await getAdminStats(novemberRequest)).toEqual(novemberStats)
  })

  it('keeps the null previous range and the null previous numbers of a request for everything', async () => {
    stubFetch(Response.json(allTimeStats))

    const stats = await getAdminStats({ by: 'month' })

    expect(stats.previousFrom).toBeNull()
    expect(stats.previousTo).toBeNull()
    expect(stats.signUps.previousTotal).toBeNull()
    expect(stats.activePeople.previousPeople).toBeNull()
    expect(stats.activePeople.previousPersonDays).toBeNull()
    expect(stats.expensesAdded.previousTotal).toBeNull()
  })

  it('keeps a zero everywhere, as an empty range answers it', async () => {
    const empty = statsWith({
      allTime: { accounts: 0, guests: 0, groups: 0, expenses: 0, payments: 0 },
      signUps: { total: 0, previousTotal: 0, counts: [0, 0, 0, 0], email: 0, google: 0, guest: 0, guestsUpgraded: 0 },
      activePeople: {
        people: 0,
        previousPeople: 0,
        personDays: 0,
        previousPersonDays: 0,
        guests: 0,
        peopleCounts: [0, 0, 0, 0],
        personDayCounts: [0, 0, 0, 0],
      },
    })
    stubFetch(Response.json(empty))

    expect(await getAdminStats(novemberRequest)).toEqual(empty)
  })

  it('throws the ApiError with its code when the server refuses the range', async () => {
    stubFetch(problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG'))

    const error = await captureError(getAdminStats(novemberRequest))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'ADMIN_STATS_DAY_RANGE_TOO_LONG' })
  })

  it('throws the ApiError with status 403 for a person who is not an admin', async () => {
    stubFetch(new Response(null, { status: 403 }))

    const error = await captureError(getAdminStats(novemberRequest))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403 })
  })

  it.each(['null', 'a text'] as const)('throws an error saying an object was expected when the answer is %s', async (kind) => {
    stubFetch(Response.json(kind === 'null' ? null : 'Healthy'))

    await expect(getAdminStats(novemberRequest)).rejects.toThrow('JSON object')
  })

  it.each([
    'from',
    'to',
    'by',
    'previousFrom',
    'previousTo',
    'buckets',
    'allTime',
    'signUps',
    'activePeople',
    'joinedViaInvite',
    'groupsCreated',
    'expensesAdded',
    'paymentsConfirmed',
  ])('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(novemberStats, field)))

    await expect(getAdminStats(novemberRequest)).rejects.toThrow(`"${field}"`)
  })

  it.each([
    ['from', 20261101],
    ['from', null],
    ['to', true],
    ['to', null],
    ['by', 'quarter'],
    ['by', 'DAY'],
    ['by', 1],
    ['by', null],
    ['previousFrom', 20261028],
    ['previousTo', false],
    ['buckets', null],
    ['buckets', 'x'],
    ['buckets', [20261101]],
    ['buckets', ['2026-11-01', null]],
    ['allTime', null],
    ['allTime', 'x'],
    ['allTime', []],
    ['signUps', null],
    ['activePeople', 7],
    ['joinedViaInvite', []],
    ['groupsCreated', null],
    ['expensesAdded', 'x'],
    ['paymentsConfirmed', 3],
  ])('throws an error naming "%s" when it is %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ [field]: value })))

    await expect(getAdminStats(novemberRequest)).rejects.toThrow(`"${field}"`)
  })

  it.each([
    ['allTime', ['accounts', 'guests', 'groups', 'expenses', 'payments']],
    ['signUps', ['total', 'previousTotal', 'counts', 'email', 'google', 'guest', 'guestsUpgraded']],
    [
      'activePeople',
      ['people', 'previousPeople', 'personDays', 'previousPersonDays', 'guests', 'peopleCounts', 'personDayCounts'],
    ],
    ['joinedViaInvite', ['total', 'previousTotal', 'counts']],
    ['groupsCreated', ['total', 'previousTotal', 'counts']],
    ['expensesAdded', ['total', 'previousTotal', 'counts']],
    ['paymentsConfirmed', ['total', 'previousTotal', 'counts']],
  ] as const)('throws an error naming the field when a part of %s lacks it', async (section, fields) => {
    for (const field of fields) {
      stubFetch(Response.json(bodyWithoutSectionField(section, field)))

      await expect(getAdminStats(novemberRequest), `${section}.${field}`).rejects.toThrow(`"${field}"`)
    }
  })

  it.each([
    ['allTime', 'accounts', '58'],
    ['allTime', 'guests', -1],
    ['allTime', 'groups', 1.5],
    ['allTime', 'expenses', null],
    ['allTime', 'payments', Number.MAX_SAFE_INTEGER + 1],
    ['signUps', 'total', '9'],
    ['signUps', 'total', -1],
    ['signUps', 'previousTotal', 'x'],
    ['signUps', 'previousTotal', -2],
    ['signUps', 'counts', null],
    ['signUps', 'counts', [1, '2']],
    ['signUps', 'counts', [1, -2]],
    ['signUps', 'counts', [1, 2.5]],
    ['signUps', 'email', true],
    ['signUps', 'google', null],
    ['signUps', 'guest', 'x'],
    ['signUps', 'guestsUpgraded', 0.5],
    ['activePeople', 'people', '21'],
    ['activePeople', 'previousPeople', 'x'],
    ['activePeople', 'personDays', null],
    ['activePeople', 'previousPersonDays', -1],
    ['activePeople', 'guests', 1.5],
    ['activePeople', 'peopleCounts', {}],
    ['activePeople', 'personDayCounts', [1, null]],
    ['joinedViaInvite', 'total', null],
    ['groupsCreated', 'previousTotal', '0'],
    ['expensesAdded', 'counts', 'x'],
    ['paymentsConfirmed', 'counts', [3, -3]],
  ] as const)('throws an error naming "%s" part "%s" when it is %j', async (section, field, value) => {
    stubFetch(Response.json(bodyWithSection(section, { [field]: value })))

    await expect(getAdminStats(novemberRequest)).rejects.toThrow(`"${field}"`)
  })
})
