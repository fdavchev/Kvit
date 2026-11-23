import { useState } from 'react'
import { hasErrorCode } from '@/core/api/errors'

export type RefusedCodesOfField<Field extends string> = Readonly<Record<Field, readonly string[]>>

export interface FieldRefusals<Field extends string> {
  isRefused: (field: Field) => boolean
  noteChange: (field: Field) => void
  noteSave: () => void
}

export function useFieldRefusals<Field extends string>(
  refusedCodesOf: RefusedCodesOfField<Field>,
  error: unknown,
): FieldRefusals<Field> {
  const [changedSinceSave, setChangedSinceSave] = useState<ReadonlySet<Field>>(() => new Set())

  return {
    isRefused: (field) =>
      !changedSinceSave.has(field) && refusedCodesOf[field].some((code) => hasErrorCode(error, code)),
    noteChange: (field) =>
      setChangedSinceSave((changed) => (changed.has(field) ? changed : new Set([...changed, field]))),
    noteSave: () => setChangedSinceSave(new Set()),
  }
}
