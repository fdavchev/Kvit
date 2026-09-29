import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import { pingHealth } from '@/core/services/health/healthService'

export function useApiHealth(): void {
  const { error } = useQuery({ queryKey: ['health'], queryFn: pingHealth })

  useEffect(() => {
    if (error !== null) {
      console.error('The health ping failed', error)
    }
  }, [error])
}
