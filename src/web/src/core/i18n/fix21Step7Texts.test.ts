import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'

const latinLettersApartFromKvit = /[A-Za-zÀ-ɏ]/

const fix21Step7Texts: [string, string, string][] = [
  ['errors.SERVER_ERROR', 'Something went wrong on our side. Try again in a moment.', 'Нешто тргна наопаку кај нас. Обиди се повторно за малку.'],
  ['errors.CHANGED_MEANWHILE', 'Someone changed this at the same moment. Try again.', 'Некој го смени ова во истиот момент. Пробај пак.'],
  ['errors.REQUEST_TOO_LARGE', 'This is too large to send. Make it shorter and try again.', 'Ова е преголемо за испраќање. Скрати го и обиди се повторно.'],
  ['errors.REQUEST_INVALID', 'The server could not read what the app sent. Reload the page and try again.', 'Серверот не можеше да го прочита она што го испрати апликацијата. Вчитај ја страницата повторно и обиди се пак.'],
]

describe('the Fix 21 Step 7 texts', () => {
  it.each(fix21Step7Texts)('has the English and the draft Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(fix21Step7Texts)('writes the Macedonian text of %s with Cyrillic letters only', (key) => {
    expect(translated('mk', key)).not.toMatch(latinLettersApartFromKvit)
  })

  it('tells a server crash apart from the generic text in both languages', () => {
    expect(translated('en', 'errors.SERVER_ERROR')).not.toBe(translated('en', 'errors.generic'))
    expect(translated('mk', 'errors.SERVER_ERROR')).not.toBe(translated('mk', 'errors.generic'))
  })
})
