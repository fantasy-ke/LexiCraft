import http from '@/utils/http.ts'
import type { Dict } from '@/types/types.ts'

export function wordDelete(params?, data?) {
  return http<Dict>('word/delete', data, params, 'post')
}
