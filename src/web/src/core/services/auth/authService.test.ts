import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import {
  changePassword,
  googleLogIn,
  googleSignUp,
  logIn,
  logOut,
  register,
  setPassword,
} from './authService'

const registration = {
  displayName: 'Filip',
  email: 'filip@example.com',
  password: 'Passw0rdOk',
  language: 'mk',
} as const

const credentials = { email: 'filip@example.com', password: 'Passw0rdOk' }

const googleSignUpInput = {
  idToken: 'header.payload.signature',
  displayName: 'Марко',
  language: 'mk',
} as const

const passwords = { currentPassword: 'Temp0rary1', newPassword: 'Passw0rdOk' }

function stubDeviceTimeZone(timeZone: string | undefined): void {
  const options = new Intl.DateTimeFormat().resolvedOptions()
  vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue(
    Object.assign({}, options, { timeZone }),
  )
}

describe('authService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('register posts the name, email, password, language and the device time zone as JSON and returns the new person', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone('Europe/Skopje')

    const me = await register(registration)

    expect(me).toEqual(testMe)
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/register',
      method: 'POST',
      contentType: 'application/json',
      body: {
        displayName: 'Filip',
        email: 'filip@example.com',
        password: 'Passw0rdOk',
        timeZone: 'Europe/Skopje',
        language: 'mk',
      },
    })
  })

  it('logIn posts the email, password and the device time zone as JSON and returns the person', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone('Europe/Skopje')

    const me = await logIn(credentials)

    expect(me).toEqual(testMe)
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/login',
      method: 'POST',
      contentType: 'application/json',
      body: {
        email: 'filip@example.com',
        password: 'Passw0rdOk',
        timeZone: 'Europe/Skopje',
      },
    })
  })

  it.each([
    ['register', () => register(registration)],
    ['logIn', () => logIn(credentials)],
    ['googleLogIn', () => googleLogIn(googleSignUpInput.idToken)],
    ['googleSignUp', () => googleSignUp(googleSignUpInput)],
  ])('%s reads the device time zone when it is called, not when the module loads', async (_name, send) => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone('Pacific/Kiritimati')

    await send()

    expect(sentRequest(fetchMock).body).toMatchObject({ timeZone: 'Pacific/Kiritimati' })
  })

  it.each([
    ['register', () => register(registration)],
    ['logIn', () => logIn(credentials)],
    ['googleLogIn', () => googleLogIn(googleSignUpInput.idToken)],
    ['googleSignUp', () => googleSignUp(googleSignUpInput)],
  ])('%s still sends the request with an empty time zone when the device gives none', async (_name, send) => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone(undefined)

    await send()

    expect(sentRequest(fetchMock).body).toMatchObject({ timeZone: '' })
  })

  it('googleLogIn posts the Google ID token and the device time zone as JSON and returns the person', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone('Europe/Skopje')

    const me = await googleLogIn(googleSignUpInput.idToken)

    expect(me).toEqual(testMe)
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/google',
      method: 'POST',
      contentType: 'application/json',
      body: { idToken: googleSignUpInput.idToken, timeZone: 'Europe/Skopje' },
    })
  })

  it('googleSignUp posts the ID token, name, device time zone and language as JSON and returns the new person', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    stubDeviceTimeZone('Europe/Skopje')

    const me = await googleSignUp(googleSignUpInput)

    expect(me).toEqual(testMe)
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/google/sign-up',
      method: 'POST',
      contentType: 'application/json',
      body: {
        idToken: googleSignUpInput.idToken,
        displayName: 'Марко',
        timeZone: 'Europe/Skopje',
        language: 'mk',
      },
    })
  })

  it('logOut posts to /api/auth/logout and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await logOut()

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/auth/logout',
      method: 'POST',
    })
  })

  it('changePassword posts the current and new password as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await changePassword(passwords)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/change-password',
      method: 'POST',
      contentType: 'application/json',
      body: passwords,
    })
  })

  it('setPassword posts the new password as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await setPassword(passwords.newPassword)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/auth/set-password',
      method: 'POST',
      contentType: 'application/json',
      body: { newPassword: passwords.newPassword },
    })
  })

  it.each([
    ['register', () => register(registration)],
    ['logIn', () => logIn(credentials)],
    ['logOut', () => logOut()],
    ['changePassword', () => changePassword(passwords)],
    ['googleLogIn', () => googleLogIn(googleSignUpInput.idToken)],
    ['googleSignUp', () => googleSignUp(googleSignUpInput)],
    ['setPassword', () => setPassword(passwords.newPassword)],
  ])('%s rethrows the ApiError unchanged', async (_name, send) => {
    stubFetch(problemResponse(400, 'AUTH_PASSWORD_TOO_WEAK'))

    const error = await captureError(send())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'AUTH_PASSWORD_TOO_WEAK' })
  })

  it.each([
    ['register', () => register(registration)],
    ['logIn', () => logIn(credentials)],
    ['googleLogIn', () => googleLogIn(googleSignUpInput.idToken)],
    ['googleSignUp', () => googleSignUp(googleSignUpInput)],
  ])('%s checks the answer with the same shape check as getMe', async (_name, send) => {
    stubFetch(Response.json({ ...testMe, language: 'fr' }))

    await expect(send()).rejects.toThrow('language')
  })
})
