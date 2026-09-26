import { useQuery } from '@tanstack/react-query'
import { pingHealth } from '@/core/services/health/healthService'

export function useApiHealth(): void {
  useQuery({ queryKey: ['health'], queryFn: pingHealth })
}
