import { useEffect } from 'react'
import { recordActive } from '@/core/services/dashboard/dashboardService'

export function useRecordActive(): void {
  useEffect(() => {
    recordActive().catch((error: unknown) => {
      console.error('Could not record that the signed-in person used the app today', error)
    })
  }, [])
}
