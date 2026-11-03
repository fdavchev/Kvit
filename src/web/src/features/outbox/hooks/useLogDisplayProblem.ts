import { useEffect } from 'react'

export function useLogDisplayProblem(problem: string | null): void {
  useEffect(() => {
    if (problem !== null) {
      console.error(problem)
    }
  }, [problem])
}
