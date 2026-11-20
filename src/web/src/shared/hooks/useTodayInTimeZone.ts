import { useEffect, useState } from 'react'
import { todayInTimeZone } from '@/shared/utils/formatDate'

export function useTodayInTimeZone(timeZone: string): string {
  const [today, setToday] = useState<string>(() => todayInTimeZone(timeZone, new Date()))

  useEffect(() => {
    function refreshToday(): void {
      if (document.visibilityState === 'visible') {
        setToday(todayInTimeZone(timeZone, new Date()))
      }
    }
    refreshToday()
    document.addEventListener('visibilitychange', refreshToday)
    window.addEventListener('focus', refreshToday)
    return () => {
      document.removeEventListener('visibilitychange', refreshToday)
      window.removeEventListener('focus', refreshToday)
    }
  }, [timeZone])

  return today
}
