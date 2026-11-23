import { useEffect } from 'react'
import { recordActive } from '@/core/services/dashboard/dashboardService'
import { todayInTimeZone } from '@/shared/utils/formatDate'

const activeDayTimeZone = 'UTC'

export function useRecordActive(): void {
  useEffect(() => {
    let recordedDay = todayInTimeZone(activeDayTimeZone, new Date())
    sendActive()

    function recordWhenShownOnNewDay(): void {
      if (document.visibilityState !== 'visible') {
        return
      }
      const today = todayInTimeZone(activeDayTimeZone, new Date())
      if (today === recordedDay) {
        return
      }
      recordedDay = today
      sendActive()
    }

    document.addEventListener('visibilitychange', recordWhenShownOnNewDay)
    return () => {
      document.removeEventListener('visibilitychange', recordWhenShownOnNewDay)
    }
  }, [])
}

function sendActive(): void {
  recordActive().catch((error: unknown) => {
    console.error('Could not record that the signed-in person used the app today', error)
  })
}
