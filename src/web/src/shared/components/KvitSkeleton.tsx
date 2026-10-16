import { cn } from '@/shared/utils/cn'

interface KvitSkeletonProps {
  className: string
}

export function KvitSkeleton({ className }: KvitSkeletonProps) {
  return (
    <span
      aria-hidden="true"
      data-skeleton
      className={cn('block animate-pulse rounded-2xl bg-skeleton motion-reduce:animate-none', className)}
    />
  )
}
