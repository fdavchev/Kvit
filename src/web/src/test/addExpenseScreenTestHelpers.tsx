import { screen } from '@testing-library/react'
import { routes } from '@/core/router/routes'
import { AddExpenseScreen } from '@/features/expenses/add/components/AddExpenseScreen'
import { hotelDetail, testCategories } from './expenseTestData'
import { categoriesPath, groupPath, expensesPath, membersPath } from './expenseTestHelpers'
import { groupOf, testGroupId } from './groupTestData'
import { ownerViewMembers } from './memberTestData'
import { createDeferred } from './outboxTestHelpers'
import { holdOutboxSender } from './queuedSaveTestHelpers'
import { renderRoutesWithProviders } from './renderWithProviders'
import { jsonAnswer, stubFetchByRequest, type AnswerFactory } from './requestTestHelpers'
import { seedMe, testMe } from './testMe'
import { translated } from './translated'

export interface ReadyAddExpenseOptions {
  postAnswer?: AnswerFactory
  groupAnswer?: AnswerFactory
}

export interface HeldAnswers {
  answer: AnswerFactory
  release: (body: unknown) => void
}

export function answerFirstRequestAtOnceAndHoldTheRest(body: unknown): HeldAnswers {
  const held = createDeferred<Response>()
  let requestCount = 0
  return {
    answer: () => {
      requestCount += 1
      return requestCount === 1 ? Response.json(body) : held.promise.then((response) => response.clone())
    },
    release: (releasedBody) => held.resolve(Response.json(releasedBody)),
  }
}

export async function renderReadyAddExpense(options: ReadyAddExpenseOptions = {}) {
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: options.groupAnswer ?? jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: jsonAnswer(ownerViewMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${expensesPath}`]: options.postAnswer ?? jsonAnswer(hotelDetail),
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpenseNew(testGroupId),
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })
  const run = holdOutboxSender(rendered.outboxSender)
  return { fetchMock, run, ...rendered }
}
