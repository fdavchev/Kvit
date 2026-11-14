import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

const personId = '4b7c9d10-2e3f-4a5b-8c6d-7e8f9a0b1c2d'
const groupId = '9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d'

function queryOf(path: string, name: string): string | null {
  return new URL(path, 'http://kvit.test').searchParams.get(name)
}

function pathOf(path: string): string {
  return new URL(path, 'http://kvit.test').pathname
}

describe('the endpoints of the owner tools', () => {
  it('builds adminToolsPeople as the people address with the query', () => {
    const path = endpoints.adminToolsPeople('ana')

    expect(pathOf(path)).toBe('/api/admin/tools/people')
    expect(queryOf(path, 'query')).toBe('ana')
  })

  it.each(['ana@example.com', 'Ana Petrova', 'ана & петар?', '=#+%'])('writes the query %j so that the server reads it back unchanged', (query) => {
    expect(queryOf(endpoints.adminToolsPeople(query), 'query')).toBe(query)
  })

  it.each([
    ['adminToolsPersonSuspend', endpoints.adminToolsPersonSuspend, `/api/admin/tools/people/${personId}/suspend`],
    ['adminToolsPersonUnsuspend', endpoints.adminToolsPersonUnsuspend, `/api/admin/tools/people/${personId}/unsuspend`],
    ['adminToolsPersonEraseName', endpoints.adminToolsPersonEraseName, `/api/admin/tools/people/${personId}/erase-name`],
  ])('builds %s for one person', (_name, build, path) => {
    expect(build(personId)).toBe(path)
  })

  it.each([
    ['adminToolsGroup', endpoints.adminToolsGroup, `/api/admin/tools/groups/${groupId}`],
    ['adminToolsGroupClose', endpoints.adminToolsGroupClose, `/api/admin/tools/groups/${groupId}/close`],
    ['adminToolsGroupReopen', endpoints.adminToolsGroupReopen, `/api/admin/tools/groups/${groupId}/reopen`],
  ])('builds %s for one group', (_name, build, path) => {
    expect(build(groupId)).toBe(path)
  })

  it.each([10, 50, 200])('builds adminToolsLog with the limit %i', (limit) => {
    expect(endpoints.adminToolsLog(limit)).toBe(`/api/admin/tools/log?limit=${limit}`)
  })
})
