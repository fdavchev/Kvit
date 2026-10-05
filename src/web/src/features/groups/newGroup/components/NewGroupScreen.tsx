import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import type { Currency } from '@/shared/utils/formatMoney'
import { GroupFields } from '../../shared/components/GroupFields'
import { defaultGroupEmoji } from '../../shared/groupEmojis'
import { useCreateGroup } from '../hooks/useCreateGroup'

export function NewGroupScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createGroup = useCreateGroup()
  const [name, setName] = useState('')
  const [emoji, setEmoji] = useState(defaultGroupEmoji)
  const [currency, setCurrency] = useState<Currency>('MKD')
  const [isNameMissing, setIsNameMissing] = useState<boolean>(false)

  function submit(): void {
    if (name.trim() === '') {
      setIsNameMissing(true)
      createGroup.reset()
      return
    }
    setIsNameMissing(false)
    createGroup.mutate(
      { name, emoji, currency },
      {
        onSuccess: (group) => {
          navigate(routes.group(group.id), { replace: true })
        },
      },
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.groups} />
      </div>
      <KvitScreenTitle>{t('newGroup.title')}</KvitScreenTitle>
      <KvitForm
        submitLabel={t('newGroup.create')}
        isPending={createGroup.isPending}
        errorMessage={createGroup.isError ? t(errorMessageKey(createGroup.error)) : null}
        onSubmit={submit}
      >
        <GroupFields
          name={name}
          emoji={emoji}
          currency={currency}
          nameInvalid={isNameMissing ? t('groupFields.nameInvalid') : null}
          onNameChange={setName}
          onEmojiChange={setEmoji}
          onCurrencyChange={setCurrency}
        />
      </KvitForm>
    </KvitScreen>
  )
}
