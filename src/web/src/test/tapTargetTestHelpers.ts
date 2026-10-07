import { expect } from 'vitest'

export const tapTargetClasses: readonly string[] = ['min-h-11', 'min-w-11']

export function expectTapTarget(element: HTMLElement, description: string): void {
  const classesOfElement: string[] = Array.from(element.classList)
  for (const requiredClass of tapTargetClasses) {
    expect(
      classesOfElement,
      `${description} must carry the class ${requiredClass} so that its tap area is at least 44 px, but its classes are: ${element.className}`,
    ).toContain(requiredClass)
  }
}
