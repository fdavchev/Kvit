import { screen, within } from '@testing-library/react'
import type { ReactElement } from 'react'
import { describe, expect, it } from 'vitest'
import type { ExpensePerson } from '@/features/expenses/shared/expensePeople'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import type { BalanceCardRow, ConfirmedPaymentView, NeedsYouView } from '../balancesView'
import { BalanceCard } from './BalanceCard'
import { NeedsYouCard } from './NeedsYouCard'
import { PaymentRow } from './PaymentRow'
import { WaitingPaymentRow } from './WaitingPaymentRow'

const longName = 'Александра-Христина Миладиновска-Јовановска'
const groupId = '0d3b9f5e-0000-4000-8000-000000000001'
const settlementId = 'c0000000-0000-4000-8000-0000000000aa'

const longNamedPerson: ExpensePerson = {
  memberId: 'a0000000-0000-4000-8000-000000000001',
  name: longName,
  pictureUrl: null,
  colorIndex: 2,
  isYou: false,
  isCurrentMember: true,
}

const meAsPerson: ExpensePerson = {
  memberId: 'a0000000-0000-4000-8000-000000000002',
  name: 'Filip',
  pictureUrl: null,
  colorIndex: 0,
  isYou: true,
  isCurrentMember: true,
}

const confirmedPayment: ConfirmedPaymentView = {
  settlementId,
  payer: longNamedPerson,
  receiver: meAsPerson,
  amountMinor: 33300,
  currency: 'MKD',
  createdAt: '2026-10-05T09:00:00Z',
  canDelete: true,
}

const needsYouItem: NeedsYouView = {
  settlementId,
  payer: longNamedPerson,
  receiverName: 'Filip',
  amountMinor: 33300,
  currency: 'MKD',
  createdAt: '2026-10-05T09:00:00Z',
}

const paysYouRow: BalanceCardRow = {
  kind: 'paysYou',
  key: 'pays-you',
  other: longNamedPerson,
  amountMinor: 33300,
  payment: { payer: longNamedPerson, receiver: meAsPerson, amountMinor: 33300, currency: 'MKD' },
}

const youPayRow: BalanceCardRow = {
  kind: 'youPay',
  key: 'you-pay',
  other: longNamedPerson,
  amountMinor: 33300,
  payment: { payer: meAsPerson, receiver: longNamedPerson, amountMinor: 33300, currency: 'MKD' },
}

function balanceCardWith(row: BalanceCardRow): ReactElement {
  return (
    <BalanceCard
      groupId={groupId}
      title="Your balance"
      card={{
        currency: 'MKD',
        myBalanceMinor: 33300,
        owedLine: { kind: 'oweYou', names: [longName] },
        rows: [row],
      }}
      language="en"
      canChangePayments
      onMarkPaid={() => {}}
    />
  )
}

const rowCases: { name: string; element: () => ReactElement; buttonName: string }[] = [
  {
    name: 'PaymentRow',
    element: () => (
      <ul>
        <PaymentRow groupId={groupId} payment={confirmedPayment} detail="333 MKD" canDelete />
      </ul>
    ),
    buttonName: 'Delete',
  },
  {
    name: 'WaitingPaymentRow',
    element: () => (
      <ul>
        <WaitingPaymentRow
          groupId={groupId}
          settlementId={settlementId}
          receiver={longNamedPerson}
          title={`You paid ${longName} 333 MKD`}
          detail={`Waiting for ${longName}`}
          canCancel
        />
      </ul>
    ),
    buttonName: 'Cancel',
  },
  {
    name: 'BalanceCard row that says the person pays you',
    element: () => balanceCardWith(paysYouRow),
    buttonName: 'Mark paid',
  },
  {
    name: 'BalanceCard row that says you pay the person',
    element: () => balanceCardWith(youPayRow),
    buttonName: 'Mark paid',
  },
]

async function renderedRowParts(element: ReactElement, buttonName: string) {
  await renderElementWithProviders(element, '/rows')
  const row = screen.getAllByRole('listitem').at(-1)
  if (row === undefined) {
    throw new Error('Expected the rendered component to contain a list item, found none')
  }
  const nameText = row.querySelector('p')
  if (nameText === null || nameText.parentElement === null || nameText.parentElement.parentElement === null) {
    throw new Error(`Expected the row to have a text with a container and a row container: ${row.outerHTML}`)
  }
  const textContainer = nameText.parentElement
  const rowContainer = textContainer.parentElement
  if (rowContainer === null) {
    throw new Error(`Expected the text container to sit inside a row container: ${row.outerHTML}`)
  }
  const button = within(row).getByRole('button', { name: buttonName })
  return { nameText, textContainer, rowContainer, button }
}

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

describe('rows of Who pays whom with a very long name', () => {
  describe.each(rowCases)('$name', ({ element, buttonName }) => {
    it('lets the row wrap, so the action button can drop under the text', async () => {
      const { rowContainer } = await renderedRowParts(element(), buttonName)

      expect(classesOf(rowContainer)).toContain('flex-wrap')
    })

    it('gives the text container a base width of 10rem that can still shrink and grow', async () => {
      const { textContainer } = await renderedRowParts(element(), buttonName)

      expect(classesOf(textContainer)).toEqual(expect.arrayContaining(['min-w-0', 'flex-1', 'basis-40']))
    })

    it('breaks the name only at word edges, never letter by letter', async () => {
      const { nameText } = await renderedRowParts(element(), buttonName)

      expect(classesOf(nameText)).toContain('break-words')
      expect(classesOf(nameText)).not.toContain('wrap-anywhere')
    })

    it('keeps the action button at its own size', async () => {
      const { button } = await renderedRowParts(element(), buttonName)

      expect(classesOf(button)).toContain('flex-none')
    })
  })

  describe('NeedsYouCard', () => {
    async function renderedNeedsYouCard() {
      await renderElementWithProviders(
        <ul>
          <NeedsYouCard
            groupId={groupId}
            item={needsYouItem}
            sentence={`${longName} says they paid you 333 MKD`}
            howLongAgo="2 hours ago"
            canAnswer
          />
        </ul>,
        '/rows',
      )
      const card = screen.getByRole('listitem')
      const sentenceText = card.querySelector('p')
      if (sentenceText === null || sentenceText.parentElement === null) {
        throw new Error(`Expected the card to have a sentence with a container: ${card.outerHTML}`)
      }
      const avatarRow = sentenceText.parentElement.parentElement
      if (avatarRow === null) {
        throw new Error(`Expected the sentence container to sit inside a row: ${card.outerHTML}`)
      }
      return { sentenceText, textContainer: sentenceText.parentElement, avatarRow }
    }

    it('gives the sentence container a base width of 10rem that can still shrink and grow', async () => {
      const { textContainer } = await renderedNeedsYouCard()

      expect(classesOf(textContainer)).toEqual(expect.arrayContaining(['min-w-0', 'flex-1', 'basis-40']))
    })

    it('breaks the sentence only at word edges, never letter by letter', async () => {
      const { sentenceText } = await renderedNeedsYouCard()

      expect(classesOf(sentenceText)).toContain('break-words')
      expect(classesOf(sentenceText)).not.toContain('wrap-anywhere')
    })

    it('lets the avatar and sentence row wrap', async () => {
      const { avatarRow } = await renderedNeedsYouCard()

      expect(classesOf(avatarRow)).toContain('flex-wrap')
    })
  })
})
