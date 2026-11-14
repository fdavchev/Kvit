import { useMutation, type UseMutationResult } from '@tanstack/react-query'
import { exportMyData } from '@/core/services/me/meService'
import { saveBlobAsFile } from '@/shared/utils/saveBlobAsFile'

export function useDownloadMyData(): UseMutationResult<void, Error, void> {
  return useMutation({
    mutationFn: async () => {
      const { blob, fileName } = await exportMyData()
      saveBlobAsFile(blob, fileName)
    },
  })
}
