import { supportEmail } from '@/core/legal/legalInfo'
import { kvitInlineLinkHitArea } from '@/shared/components/kvitInlineLinkLook'
import { cn } from '@/shared/utils/cn'

interface TextWithEmailLinksProps {
  text: string
}

export function TextWithEmailLinks({ text }: TextWithEmailLinksProps) {
  return text.split(supportEmail).flatMap((part, index) =>
    index === 0
      ? [part]
      : [
          <a
            key={index}
            href={`mailto:${supportEmail}`}
            className={cn('font-semibold text-link hover:underline', kvitInlineLinkHitArea)}
          >
            {supportEmail}
          </a>,
          part,
        ],
  )
}
