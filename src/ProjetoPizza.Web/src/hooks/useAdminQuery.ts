import { useQueryClient, useSuspenseQuery, type QueryKey } from '@tanstack/react-query'
import { useCallback } from 'react'

type DataUpdater<T> = T | ((current: T) => T)

export function useAdminQuery<T>(
  queryKey: QueryKey,
  queryFn: (signal?: AbortSignal) => Promise<T>,
) {
  const queryClient = useQueryClient()
  const query = useSuspenseQuery({
    queryKey,
    queryFn: ({ signal }) => queryFn(signal),
  })

  const setData = useCallback((updater: DataUpdater<T>) => {
    queryClient.setQueryData<T>(queryKey, (current) => {
      if (typeof updater === 'function') {
        return (updater as (value: T) => T)(current ?? query.data)
      }
      return updater
    })
  }, [query.data, queryClient, queryKey])

  return {
    data: query.data,
    setData,
    refresh: query.refetch,
    isRefreshing: query.isFetching,
  }
}
