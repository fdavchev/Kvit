export function googleClientId(): string {
  const clientId: string | undefined = import.meta.env.VITE_GOOGLE_CLIENT_ID
  if (clientId === undefined || clientId.trim() === '') {
    throw new Error(
      `VITE_GOOGLE_CLIENT_ID is missing or empty (got ${JSON.stringify(clientId)}); it is set in src/web/.env`,
    )
  }
  return clientId
}
