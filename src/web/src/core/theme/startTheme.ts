import { applyTheme } from './applyTheme'
import { readSavedTheme } from './savedTheme'

export function startTheme(): void {
  applyTheme(readSavedTheme())
}
