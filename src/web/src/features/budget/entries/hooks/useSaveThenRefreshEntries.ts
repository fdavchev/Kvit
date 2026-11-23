import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../personalEntryCache'

export function useSaveThenRefreshEntries<Input>(
  save: (input: Input) => Promise<void>,
  oneAtATime: SaveOnce,
): SaveRun<Input> {
  const queryClient = useQueryClient()
  const mutation = useMutation<void, Error, Input>({
    mutationFn: save,
    onSuccess: () => refreshPersonalEntries(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (input, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(input, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
