import { supportEmail } from '@/core/legal/legalInfo'

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
            className="font-semibold text-link hover:underline"
          >
            {supportEmail}
          </a>,
          part,
        ],
  )
}
