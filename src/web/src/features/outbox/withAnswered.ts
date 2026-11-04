export function withAnswered<Input extends { onAnswered?: () => void }, Result>(
  call: (input: Omit<Input, 'onAnswered'>) => Promise<Result>,
): (input: Input) => Promise<Result> {
  return async ({ onAnswered, ...input }) => {
    const result = await call(input)
    onAnswered?.()
    return result
  }
}
