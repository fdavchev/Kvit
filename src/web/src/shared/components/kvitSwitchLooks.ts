export const kvitSwitchLooks = {
  track: 'flex rounded-full bg-track p-[3px]',
  option:
    'pressable transition-[scale] duration-100 ease-out motion-reduce:transition-none relative h-[38px] min-w-12 rounded-full px-3 text-[0.9375rem] font-semibold text-muted-foreground after:absolute after:inset-x-0 after:-inset-y-[3px] hover:text-foreground aria-pressed:bg-track-active aria-pressed:text-foreground aria-pressed:shadow-[0_1px_3px_var(--track-shadow)]',
} as const
