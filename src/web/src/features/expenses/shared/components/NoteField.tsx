import { useId } from 'react'
import { useTranslation } from 'react-i18next'

interface NoteFieldProps {
  value: string
  onChange: (note: string) => void
  invalid?: boolean
  autoFocus?: boolean
}

export function NoteField({ value, onChange, invalid, autoFocus }: NoteFieldProps) {
  const { t } = useTranslation()
  const noteId = useId()

  return (
    <div className="flex flex-col">
      <label htmlFor={noteId} className="mb-2 text-[0.9375rem] font-bold">
        {t('expense.note')}
      </label>
      <textarea
        id={noteId}
        value={value}
        rows={3}
        autoFocus={autoFocus}
        aria-invalid={invalid}
        onChange={(event) => onChange(event.target.value)}
        className="min-h-24 w-full rounded-[14px] border-2 border-field-border bg-field px-4 py-3 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0 aria-invalid:border-destructive"
      />
    </div>
  )
}
