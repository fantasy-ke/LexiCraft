import type {ResultDto} from '@/types/api'
import {serviceGet, servicePost} from '@/utils/authHttp'

export interface WordListDto {
    id: number
    name: string
    category: string | null
    description: string | null
}

export interface WordDto {
    id: number
    spelling: string
    phonetic: string | null
    pronunciationUrl: string | null
    definitions: string | null
    examples: string | null
    tags: string[]
}

export interface PagedWordResult {
    total: number
    pageIndex: number
    pageSize: number
    seed: string | null
    data: WordDto[]
}

export interface WordImportDto {
    spelling: string
    phonetic?: string | null
    pronunciationUrl?: string | null
    definitions?: string | null
    examples?: string | null
    tags?: string[] | null
}

export interface ImportWordsRequest {
    name: string
    category?: string | null
    description?: string | null
    words: WordImportDto[]
}

export interface WordDuplicateInfo {
    spelling: string
    id: number
}

export interface WordImportResult {
    wordListId: number
    totalProcessed: number
    newWordsCount: number
    existingWordsCount: number
    deployedWords: WordDuplicateInfo[]
}

export type WordState = 'New' | 'Vague' | 'Mastered'

export interface UpdateWordStateRequest {
    userId: string
    wordId: number
    state: WordState
    isInWordBook?: boolean | null
    masteryScore?: number | null
}

export function getWordLists(category?: string): Promise<ResultDto<WordListDto[]>> {
    return serviceGet<WordListDto[]>('/vocabulary/v1/word-lists', category ? {category} : undefined)
}

export function getWordsByList(
    wordListId: number,
    params: {pageIndex?: number; pageSize?: number; seed?: string} = {}
): Promise<ResultDto<PagedWordResult>> {
    return serviceGet<PagedWordResult>(`/vocabulary/v1/word-lists/${wordListId}/words`, params)
}

export function searchWords(keyword: string): Promise<ResultDto<WordDto[]>> {
    return serviceGet<WordDto[]>('/vocabulary/v1/words', {keyword})
}

export function getWeakWords(userId: string): Promise<ResultDto<WordDto[]>> {
    return serviceGet<WordDto[]>('/vocabulary/v1/user-words/weak', {userId})
}

export function updateWordState(request: UpdateWordStateRequest): Promise<ResultDto<boolean>> {
    return servicePost<boolean>('/vocabulary/v1/user-words/state', request)
}

export function importWords(request: ImportWordsRequest): Promise<ResultDto<WordImportResult>> {
    return servicePost<WordImportResult>('/vocabulary/v1/word-lists/import', request)
}
