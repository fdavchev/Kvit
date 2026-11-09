import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { OwnerToolsGroups } from './OwnerToolsGroups'
import { OwnerToolsLog } from './OwnerToolsLog'
import { OwnerToolsPeople } from './OwnerToolsPeople'

const ownerToolsTabs = ['people', 'groups', 'log'] as const

type OwnerToolsTab = (typeof ownerToolsTabs)[number]

export function OwnerToolsScreen() {
  const { t } = useTranslation()
  const [tab, setTab] = useState<OwnerToolsTab>('people')

  return (
    <KvitScreen>
      <KvitScreenTitle>{t('ownerTools.title')}</KvitScreenTitle>
      <KvitChoiceChips<OwnerToolsTab>
        label={t('ownerTools.title')}
        labelClassName="sr-only"
        options={ownerToolsTabs.map((choice) => ({ value: choice, label: t(`ownerTools.tabs.${choice}`) }))}
        value={tab}
        onChange={setTab}
      />
      <div className="flex flex-col gap-5 pt-6 pb-6">
        {tab === 'people' && <OwnerToolsPeople />}
        {tab === 'groups' && <OwnerToolsGroups />}
        {tab === 'log' && <OwnerToolsLog />}
      </div>
    </KvitScreen>
  )
}
