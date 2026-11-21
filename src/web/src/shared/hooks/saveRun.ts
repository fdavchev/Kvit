export interface SaveRun<Input> {
  run: (input: Input, onDone: () => void, onRefused: (error: Error) => void) => void
  isPending: boolean
  error: Error | null
}
