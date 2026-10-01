import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'

interface KvitBackButtonProps {
  to: string
}

export function KvitBackButton({ to }: KvitBackButtonProps) {
  const { t } = useTranslation()
  return (
    <Link
      to={to}
      className="pressable -ml-3 inline-flex min-h-12 items-center gap-1 self-start rounded-xl px-3 font-semibold text-link hover:underline"
    >
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        className="size-5"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M15 18l-6-6 6-6" />
      </svg>
      {t('common.back')}
    </Link>
  )
}
