export const groupNameMaxLength: number = 60

export function isValidGroupName(name: string): boolean {
  const trimmedName: string = name.trim()
  return trimmedName !== '' && trimmedName.length <= groupNameMaxLength
}
