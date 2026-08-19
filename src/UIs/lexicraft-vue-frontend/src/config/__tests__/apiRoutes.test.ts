import {describe, expect, it} from 'vitest'
import {API_PREFIXES, API_ROUTES, buildGatewayRoute} from '../apiRoutes'

describe('gateway API route contract', () => {
    it('builds versioned service paths through the public gateway prefixes', () => {
        expect(API_PREFIXES).toEqual({
            identity: '/identity/v1',
            vocabulary: '/vocabulary/v1',
            practice: '/practice/v1',
            files: '/files'
        })
        expect(buildGatewayRoute('identity', '/users/info/')).toBe('/identity/v1/users/info')
    })

    it('matches the verified service routes', () => {
        expect(API_ROUTES.identity.profile).toBe('/identity/v1/users/info')
        expect(API_ROUTES.identity.permissions('a/b')).toBe('/identity/v1/permissions/a%2Fb')
        expect(API_ROUTES.vocabulary.wordsByList(12)).toBe('/vocabulary/v1/word-lists/12/words')
        expect(API_ROUTES.practice.completeTask('task/1')).toBe('/practice/v1/tasks/task%2F1/complete')
        expect(API_ROUTES.files.content).toBe('/files/content')
    })
})
