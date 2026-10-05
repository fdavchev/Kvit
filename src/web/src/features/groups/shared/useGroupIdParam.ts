import { useParams } from 'react-router'

export function useGroupIdParam(): string {
  const { groupId } = useParams()
  if (groupId === undefined) {
    throw new Error('Expected the route to have a groupId parameter, found none')
  }
  return groupId
}
