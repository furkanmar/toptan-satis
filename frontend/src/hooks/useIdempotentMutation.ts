import { useMutation, UseMutationOptions, UseMutationResult } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../api/client'

/**
 * TanStack Query useMutation wrapper — her mutasyon için Idempotency-Key üretir.
 *
 * Kullanım:
 *   const mutation = useIdempotentMutation({
 *     mutationFn: (data) => ordersApi.create(data),
 *   })
 *
 * Önemli davranışlar:
 * - İlk çağrıda crypto.randomUUID() ile key üretilir, header'a eklenir.
 * - Retry'da AYNI key kullanılır → sunucu idempotent response döner.
 * - mutate() veya mutateAsync() her yeni çağrısında yeni key üretilir.
 */
export function useIdempotentMutation<TData, TError, TVariables, TContext>(
  options: UseMutationOptions<TData, TError, TVariables, TContext>
): UseMutationResult<TData, TError, TVariables, TContext> {
  // Aktif mutasyon key'i — retry'larda aynı key kalır
  const idempotencyKeyRef = useRef<string | null>(null)

  const mutationFn = async (variables: TVariables): Promise<TData> => {
    // Yeni mutasyon başlangıcında key daha önce yoksa üret.
    // Retry durumunda ref dolu gelir, aynı key tekrar kullanılır.
    if (!idempotencyKeyRef.current) {
      idempotencyKeyRef.current = crypto.randomUUID()
    }

    // Axios interceptor'dan geçmeden önce header'ı per-request ekle
    const originalFn = options.mutationFn
    if (!originalFn) throw new Error('mutationFn gerekli')

    // Geçici interceptor: sadece bu request için header inject et
    const interceptorId = api.interceptors.request.use(config => {
      config.headers['Idempotency-Key'] = idempotencyKeyRef.current!
      return config
    })

    try {
      const result = await originalFn(variables)
      return result as TData
    } finally {
      api.interceptors.request.eject(interceptorId)
    }
  }

  return useMutation<TData, TError, TVariables, TContext>({
    ...options,
    mutationFn,
    onSettled: (data, error, variables, context) => {
      // Mutasyon tamamlandığında (başarı veya kalıcı hata) key'i sıfırla.
      // Böylece bir sonraki mutate() çağrısı yeni key alır.
      idempotencyKeyRef.current = null
      options.onSettled?.(data, error, variables, context)
    },
  })
}
