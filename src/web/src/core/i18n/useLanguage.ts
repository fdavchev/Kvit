import { useTranslation } from 'react-i18next'
import { isLanguage, type Language } from './language'
import { saveLanguage } from './savedLanguage'

export function useLanguage() {
  const { i18n } = useTranslation()
  const language = i18n.resolvedLanguage ?? ''
  if (!isLanguage(language)) {
    throw new Error(`i18next resolved an unsupported language: "${language}"`)
  }

  async function changeLanguage(newLanguage: Language): Promise<void> {
    saveLanguage(newLanguage)
    await i18n.changeLanguage(newLanguage)
  }

  return { language, changeLanguage }
}
