/**
 * useIdempotentMutation — basit useMutation wrapper.
 *
 * X-Idempotency-Key global axios interceptor tarafından eklenir (api/client.ts).
 * axios-retry aynı config'i yeniden kullandığından key, retry'larda korunur.
 * Bu hook geriye dönük uyumluluk + semantic clarity için kalıyor.
 */
export { useMutation as useIdempotentMutation } from '@tanstack/react-query'
