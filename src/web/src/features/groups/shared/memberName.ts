export const memberNameMaxLength: number = 60

export function isValidMemberName(name: string): boolean {
  const trimmedName: string = name.trim()
  return trimmedName !== '' && trimmedName.length <= memberNameMaxLength
}
