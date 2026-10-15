import { Fragment } from 'react'

const slashWord = /(\S*\/\S*)/

interface KvitUnbrokenSlashWordsProps {
  text: string
}

export function KvitUnbrokenSlashWords({ text }: KvitUnbrokenSlashWordsProps) {
  const parts: string[] = text.split(slashWord)
  if (parts.length === 1) {
    return text
  }
  return parts.map((part, index) =>
    index % 2 === 1 ? (
      <span key={index} className="whitespace-nowrap">
        {part}
      </span>
    ) : (
      <Fragment key={index}>{part}</Fragment>
    ),
  )
}
