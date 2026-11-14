import { useState } from 'react'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { TermsChangedPill } from './TermsChangedPill'
import { TermsChangedSheet } from './TermsChangedSheet'

export function TermsNotice() {
  const me = useSignedInMe()
  const [isSheetShown, setIsSheetShown] = useState<boolean>(false)
  const mustAgree = !me.termsUpToDate && me.termsUpdateIsMaterial
  if (mustAgree && !isSheetShown) {
    setIsSheetShown(true)
  }

  if (isSheetShown) {
    return <TermsChangedSheet onClose={() => setIsSheetShown(false)} />
  }
  if (!me.termsUpToDate) {
    return <TermsChangedPill />
  }
  return null
}
