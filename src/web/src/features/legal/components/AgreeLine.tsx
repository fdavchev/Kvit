import { Trans } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { routes } from '@/core/router/routes'

const linkLook = 'font-semibold text-link underline underline-offset-2'

export function AgreeLine() {
  const location = useLocation()
  const backState = { from: location.pathname }

  return (
    <p className="text-center text-[0.8125rem] leading-snug text-pretty text-muted-foreground">
      <Trans
        i18nKey="legal.agree"
        components={{
          terms: <Link to={routes.terms} state={backState} className={linkLook} />,
          privacy: <Link to={routes.privacy} state={backState} className={linkLook} />,
        }}
      />
    </p>
  )
}
