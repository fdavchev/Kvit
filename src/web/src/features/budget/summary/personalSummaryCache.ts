export function personalSummaryQueryKey(month: string): readonly ['personal', 'summary', string] {
  return ['personal', 'summary', month]
}
