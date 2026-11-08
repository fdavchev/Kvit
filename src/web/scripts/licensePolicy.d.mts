export interface LicensedPackage {
  name: string
  version: string
  license?: string | undefined
}

export const allowOverrides: Readonly<Record<string, string>>

export function findForbiddenLicenses<T extends LicensedPackage>(
  packages: readonly T[],
  overrides?: Readonly<Record<string, string>>,
): T[]

export function findWeakCopyleft<T extends LicensedPackage>(packages: readonly T[]): T[]
