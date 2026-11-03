import { describe, expect, it, vi } from 'vitest'
import { withAnswered } from './withAnswered'

interface SaveInput {
  title: string
  amountMinor: number
  onAnswered?: () => void
}

describe('withAnswered', () => {
  it('gives back the result of the call', async () => {
    const save = withAnswered<SaveInput, string>(async () => 'saved')

    await expect(save({ title: 'Dinner', amountMinor: 1200 })).resolves.toBe('saved')
  })

  it('calls onAnswered once after the call resolved', async () => {
    const onAnswered = vi.fn()
    const save = withAnswered<SaveInput, string>(async () => 'saved')

    await save({ title: 'Dinner', amountMinor: 1200, onAnswered })

    expect(onAnswered).toHaveBeenCalledTimes(1)
  })

  it('calls onAnswered after the call resolved and not before', async () => {
    const steps: string[] = []
    const save = withAnswered<SaveInput, string>(async () => {
      steps.push('call')
      return 'saved'
    })

    await save({ title: 'Dinner', amountMinor: 1200, onAnswered: () => steps.push('answered') })

    expect(steps).toEqual(['call', 'answered'])
  })

  it('does not call onAnswered while the call is still running', async () => {
    const onAnswered = vi.fn()
    let finish: (value: string) => void = () => {}
    const save = withAnswered<SaveInput, string>(
      () =>
        new Promise<string>((resolve) => {
          finish = resolve
        }),
    )

    const saving = save({ title: 'Dinner', amountMinor: 1200, onAnswered })
    await Promise.resolve()
    expect(onAnswered).not.toHaveBeenCalled()
    finish('saved')
    await saving

    expect(onAnswered).toHaveBeenCalledTimes(1)
  })

  it('never calls onAnswered when the call fails, and passes the failure on unchanged', async () => {
    const onAnswered = vi.fn()
    const failure = new Error('the server refused it')
    const save = withAnswered<SaveInput, string>(async () => {
      throw failure
    })

    await expect(save({ title: 'Dinner', amountMinor: 1200, onAnswered })).rejects.toBe(failure)

    expect(onAnswered).not.toHaveBeenCalled()
  })

  it('hands the input to the call without onAnswered', async () => {
    const call = vi.fn(async (_input: Omit<SaveInput, 'onAnswered'>) => 'saved')
    const save = withAnswered<SaveInput, string>(call)

    await save({ title: 'Dinner', amountMinor: 1200, onAnswered: () => {} })

    expect(call).toHaveBeenCalledTimes(1)
    expect(call.mock.calls[0][0]).toEqual({ title: 'Dinner', amountMinor: 1200 })
    expect('onAnswered' in call.mock.calls[0][0]).toBe(false)
  })

  it('works when there is no onAnswered', async () => {
    const call = vi.fn(async (_input: Omit<SaveInput, 'onAnswered'>) => 'saved')
    const save = withAnswered<SaveInput, string>(call)

    await expect(save({ title: 'Dinner', amountMinor: 1200 })).resolves.toBe('saved')

    expect(call.mock.calls[0][0]).toEqual({ title: 'Dinner', amountMinor: 1200 })
  })

  it('calls the call again with the new input on every use, and onAnswered once per use', async () => {
    const firstAnswered = vi.fn()
    const secondAnswered = vi.fn()
    const call = vi.fn(async (input: Omit<SaveInput, 'onAnswered'>) => input.title)
    const save = withAnswered<SaveInput, string>(call)

    await save({ title: 'First', amountMinor: 100, onAnswered: firstAnswered })
    await save({ title: 'Second', amountMinor: 200, onAnswered: secondAnswered })

    expect(call.mock.calls.map(([input]) => input.title)).toEqual(['First', 'Second'])
    expect(firstAnswered).toHaveBeenCalledTimes(1)
    expect(secondAnswered).toHaveBeenCalledTimes(1)
  })
})
