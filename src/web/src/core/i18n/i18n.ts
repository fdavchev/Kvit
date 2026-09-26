import i18next, { type i18n } from 'i18next'
import { initReactI18next } from 'react-i18next'
import { detectLanguage } from './detectLanguage'
import { languages, type Language } from './language'
import en from './locales/en.json'
import mk from './locales/mk.json'
import { readSavedLanguage } from './savedLanguage'

export async function startI18n(): Promise<i18n> {
  const language = detectLanguage(readSavedLanguage(), navigator.languages)
  const instance = await createI18n(language)
  document.documentElement.lang = language
  instance.on('languageChanged', (newLanguage) => {
    document.documentElement.lang = newLanguage
  })
  return instance
}

export async function createI18n(language: Language): Promise<i18n> {
  const instance = i18next.createInstance()
  await instance.use(initReactI18next).init({
    resources: {
      en: { translation: en },
      mk: { translation: mk },
    },
    lng: language,
    fallbackLng: 'en',
    supportedLngs: languages,
    interpolation: { escapeValue: false },
  })
  return instance
}
