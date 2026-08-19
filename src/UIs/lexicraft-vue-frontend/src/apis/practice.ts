import type {ResultDto} from '@/types/api'
import {servicePost, servicePut} from '@/utils/apiClient'
import {API_ROUTES} from '@/config/apiRoutes'

export type PracticeTaskType = 'AudioDictation' | 'MeaningDictation'
export type PracticeTaskSource = 'NewWord' | 'Review' | 'Recommendation' | 'Manual'
export type AssessmentType = 'Exact' | 'Fuzzy'
export type AnswerStatus = 'Correct' | 'Partial' | 'Wrong' | 'NoAnswer'

export interface PracticeTaskItemDto {
    wordId: string
    spelling: string
    phonetic: string
    audioUrl?: string | null
    definition: string
    index: number
}

export interface CreatePracticeTaskRequest {
    userId: string
    type: PracticeTaskType
    source: PracticeTaskSource
    category: string
    items: PracticeTaskItemDto[]
}

export interface CreatePracticeTaskResult {
    taskId: string
}

export interface SubmitAnswerRequest {
    taskId: string
    itemId: string
    userInput: string | null
    assessmentType: AssessmentType
}

export interface AssessmentResult {
    answerId: string
    status: AnswerStatus
    score: number
    correctSpelling: string
}

export function createPracticeTask(
    request: CreatePracticeTaskRequest
): Promise<ResultDto<CreatePracticeTaskResult>> {
    return servicePost<CreatePracticeTaskResult>(API_ROUTES.practice.tasks, request)
}

export function submitAnswer(request: SubmitAnswerRequest): Promise<ResultDto<AssessmentResult>> {
    return servicePost<AssessmentResult>(API_ROUTES.practice.submitAssessment, request)
}

export function completePractice(taskId: string): Promise<ResultDto<boolean>> {
    return servicePut<boolean>(API_ROUTES.practice.completeTask(taskId), {taskId})
}
