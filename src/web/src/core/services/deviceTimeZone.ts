export function readDeviceTimeZone(): string {
  return new Intl.DateTimeFormat().resolvedOptions().timeZone ?? ''
}
