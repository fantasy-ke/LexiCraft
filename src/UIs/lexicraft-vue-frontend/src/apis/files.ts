import {serviceFileGet} from '@/utils/apiClient'
import {API_ROUTES} from '@/config/apiRoutes'

/**
 * Files service currently exposes a verified HTTP content endpoint.
 * Upload remains a gRPC contract and is intentionally not guessed here.
 */
export function getContent(relativePath: string): Promise<Blob> {
    return serviceFileGet(API_ROUTES.files.content, {relativePath}).then(response => response.data)
}
