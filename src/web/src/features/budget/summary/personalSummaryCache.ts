export const personalSummaryFamilyKey = ['personal', 'summary'] as const

export function personalSummaryQueryKey(month: string): readonly ['personal', 'summary', string] {
  return [...personalSummaryFamilyKey, month]
}
