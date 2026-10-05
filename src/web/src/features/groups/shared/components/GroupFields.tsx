import { useTranslation } from 'react-i18next'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { KvitTextField } from '@/shared/components/KvitTextField'
import type { Currency } from '@/shared/utils/formatMoney'
import { groupEmojis } from '../groupEmojis'

interface GroupFieldsProps {
  name: string
  emoji: string
  currency: string
  nameInvalid?: string | null
  onNameChange: (name: string) => void
  onEmojiChange: (emoji: string) => void
  onCurrencyChange: (currency: Currency) => void
}

const emojiOptions = groupEmojis.map((emoji) => ({ value: emoji, label: emoji }))
const currencyOptions = groupCurrencies.map((currency) => ({ value: currency, label: currency }))

export function GroupFields({
  name,
  emoji,
  currency,
  nameInvalid,
  onNameChange,
  onEmojiChange,
  onCurrencyChange,
}: GroupFieldsProps) {
  const { t } = useTranslation()
  const hasNameError = typeof nameInvalid === 'string'

  return (
    <>
      <div className="flex flex-col gap-2">
        <KvitTextField
          id="group-name"
          label={t('groupFields.name')}
          value={name}
          onChange={onNameChange}
          placeholder={t('groupFields.namePlaceholder')}
          autoComplete="off"
          enterKeyHint="done"
          invalid={hasNameError}
        />
        {hasNameError && <KvitInlineError message={nameInvalid} />}
      </div>
      <KvitChoiceChips
        label={t('groupFields.emoji')}
        options={emojiOptions}
        value={emoji}
        onChange={onEmojiChange}
        chipClassName="min-w-12 px-2.5 text-[1.3rem]"
      />
      <div className="flex flex-col">
        <KvitChoiceChips
          label={t('groupFields.currency')}
          options={currencyOptions}
          value={currency}
          onChange={onCurrencyChange}
        />
        <p className="mt-2 text-[0.9375rem] leading-[1.45] text-pretty text-muted-foreground">
          {t('groupFields.currencyHint')}
        </p>
      </div>
    </>
  )
}
