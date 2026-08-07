import {serviceFileGet} from '@/utils/authHttp'

/**
 * Files service currently exposes a verified HTTP content endpoint.
 * Upload remains a gRPC contract and is intentionally not guessed here.
 */
export function getContent(relativePath: string): Promise<Blob> {
    return serviceFileGet('/files/content', {relativePath}).then(response => response.data)
}
