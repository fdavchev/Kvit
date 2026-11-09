import type {
  OwnerToolsGroup,
  OwnerToolsLogEntry,
  OwnerToolsPerson,
} from '@/core/services/ownerTools/ownerToolsService'
import { testAdminMe } from './testMe'

export const ownerToolsPath = '/api/admin/tools'
export const ownerToolsNow = new Date('2026-11-04T10:00:00Z')

export const anaPerson: OwnerToolsPerson = {
  id: '11111111-aaaa-4aaa-8aaa-111111111111',
  displayName: 'Ana Petrova',
  email: 'ana@example.com',
  isGuest: false,
  isSuspended: false,
  isAdmin: false,
  deletionPending: false,
  isDeleted: false,
  createdAt: '2026-09-01T10:00:00Z',
  groupCount: 3,
}

export const suspendedPerson: OwnerToolsPerson = {
  ...anaPerson,
  id: '22222222-bbbb-4bbb-8bbb-222222222222',
  displayName: 'Petar Ilievski',
  email: 'petar@example.com',
  isSuspended: true,
  groupCount: 1,
}

export const guestPerson: OwnerToolsPerson = {
  ...anaPerson,
  id: '33333333-cccc-4ccc-8ccc-333333333333',
  displayName: 'Grandma Mara',
  email: null,
  isGuest: true,
  groupCount: 2,
}

export const adminPerson: OwnerToolsPerson = {
  ...anaPerson,
  id: '44444444-dddd-4ddd-8ddd-444444444444',
  displayName: 'Dime Admin',
  email: 'dime@example.com',
  isAdmin: true,
  groupCount: 0,
}

export const deletingPerson: OwnerToolsPerson = {
  ...anaPerson,
  id: '55555555-eeee-4eee-8eee-555555555555',
  displayName: 'Sara Kostova',
  email: 'sara@example.com',
  deletionPending: true,
}

export const deletedPerson: OwnerToolsPerson = {
  ...anaPerson,
  id: '66666666-ffff-4fff-8fff-666666666666',
  displayName: 'Goran Stojanov',
  email: null,
  isDeleted: true,
  groupCount: 4,
}

export const personWithMyId: OwnerToolsPerson = {
  ...anaPerson,
  id: testAdminMe.id,
  displayName: 'Filip Davchev',
  email: 'filip@example.com',
}

export const tripGroup: OwnerToolsGroup = {
  id: '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e',
  name: 'Greece trip',
  emoji: '🏖️',
  status: 'Open',
  memberCount: 4,
  ownerName: 'Ana Petrova',
  createdAt: '2026-09-10T08:00:00Z',
  isDeleted: false,
  closedByAdminAt: null,
}

export const closedTripGroup: OwnerToolsGroup = { ...tripGroup, closedByAdminAt: '2026-11-03T09:00:00Z' }

export const logEntry: OwnerToolsLogEntry = {
  id: 3,
  createdAt: '2026-11-04T08:00:00Z',
  adminName: 'Filip Davchev',
  action: 'Suspended',
  targetKind: 'person',
  targetLabel: 'Ana Petrova',
  reason: 'Spam in many groups',
}

export const olderLogEntry: OwnerToolsLogEntry = {
  id: 2,
  createdAt: '2026-10-20T09:00:00Z',
  adminName: 'Filip Davchev',
  action: 'GroupClosed',
  targetKind: 'group',
  targetLabel: 'Greece trip',
  reason: 'Reported by email',
}

export const oldestLogEntry: OwnerToolsLogEntry = {
  id: 1,
  createdAt: '2026-10-01T09:00:00Z',
  adminName: 'Dime Admin',
  action: 'NameErased',
  targetKind: 'person',
  targetLabel: 'Goran Stojanov',
  reason: 'Asked by email',
}

export const logEntries: OwnerToolsLogEntry[] = [logEntry, olderLogEntry, oldestLogEntry]
