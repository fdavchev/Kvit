const sharedLook =
  'h-auto w-full rounded-2xl px-5 text-center whitespace-normal'

export const kvitButtonLooks = {
  primary: {
    shadcnVariant: 'default',
    className: `${sharedLook} min-h-14 text-lg font-bold hover:bg-primary-hover`,
  },
  secondary: {
    shadcnVariant: 'secondary',
    className: `${sharedLook} min-h-13 text-[1.0625rem] font-semibold hover:bg-secondary-hover`,
  },
  link: {
    shadcnVariant: 'link',
    className: `${sharedLook} min-h-12 text-[1.0625rem] font-semibold text-link`,
  },
  smallLink: {
    shadcnVariant: 'link',
    className: `${sharedLook} min-h-10 text-[0.9375rem] font-semibold text-link`,
  },
  underlinedLink: {
    shadcnVariant: 'link',
    className: `${sharedLook} min-h-12 text-[1.0625rem] font-bold text-foreground underline decoration-link-underline decoration-3 underline-offset-6 hover:decoration-current`,
  },
} as const

export type KvitButtonVariant = keyof typeof kvitButtonLooks
