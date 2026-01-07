# LexiCraft Practice Service API Versioning

## Overview

The LexiCraft Practice & Assessment Service API uses semantic versioning to ensure backward compatibility and smooth evolution of the API.

## Versioning Strategy

### Version Format
- **Major.Minor** format (e.g., `1.0`, `2.0`)
- Major version changes indicate breaking changes
- Minor version changes indicate backward-compatible additions

### Supported Versions
- **Current**: `v1.0` (Stable)
- **Deprecated**: None
- **Sunset**: None

### Version Specification Methods

Clients can specify the API version using any of the following methods:

1. **URL Path** (Recommended)
   ```
   GET /api/v1/practice/history
   POST /api/v1/practice/tasks/generate
   ```

2. **Header**
   ```
   api-version: 1.0
   ```

3. **Query Parameter**
   ```
   GET /api/practice/history?api-version=1.0
   ```

### Default Behavior
- If no version is specified, the API defaults to version `1.0`
- The `AssumeDefaultVersionWhenUnspecified` setting is enabled

## Backward Compatibility

### Breaking Changes Policy
Breaking changes will only be introduced in major version updates:
- Changes to request/response schemas
- Removal of endpoints or parameters
- Changes to authentication/authorization requirements
- Changes to error response formats

### Non-Breaking Changes
The following changes are considered backward-compatible and may be introduced in minor versions:
- Addition of new endpoints
- Addition of optional request parameters
- Addition of new fields to response models
- Addition of new HTTP status codes for existing endpoints
- Performance improvements

### Deprecation Process

1. **Announcement**: Deprecated features are announced 6 months before removal
2. **Warning Headers**: Deprecated endpoints return `Sunset` and `Deprecation` headers
3. **Documentation**: Deprecated features are clearly marked in API documentation
4. **Migration Guide**: Migration guides are provided for breaking changes

### Deprecation Headers

When using deprecated endpoints, the following headers are returned:

```http
Sunset: Sat, 31 Dec 2025 23:59:59 GMT
Deprecation: true
Link: <https://docs.lexicraft.com/api/deprecation-policy>; rel="deprecation"
```

## Version-Specific Documentation

Each API version maintains its own OpenAPI specification:
- **v1.0**: `/openapi/v1.json`

The Scalar documentation interface provides version-specific documentation accessible at:
- Development: `https://localhost:7003/scalar/v1`

## Migration Guidelines

### From v1.0 to Future Versions

When new major versions are released, migration guides will be provided including:
- List of breaking changes
- Code examples for common migration scenarios
- Timeline for deprecation and sunset
- Support resources

## Support Policy

### Version Support Lifecycle
- **Active**: Latest major version receives new features and bug fixes
- **Maintenance**: Previous major version receives critical bug fixes only
- **End of Life**: No longer supported

### Current Support Status
- **v1.0**: Active (Full support)

## Error Handling

### Version-Related Errors

**Unsupported Version**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.5",
  "title": "Unsupported API Version",
  "status": 400,
  "detail": "API version '2.0' is not supported. Supported versions: 1.0",
  "instance": "/api/v2/practice/tasks/generate"
}
```

**Malformed Version**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Invalid API Version Format",
  "status": 400,
  "detail": "API version must be in format 'major.minor' (e.g., '1.0')",
  "instance": "/api/practice/tasks/generate"
}
```

## Best Practices for Clients

1. **Always specify version**: Don't rely on default version behavior
2. **Use URL path versioning**: Most explicit and cache-friendly
3. **Monitor deprecation headers**: Watch for `Sunset` and `Deprecation` headers
4. **Subscribe to API updates**: Stay informed about version changes
5. **Test with new versions**: Validate compatibility before upgrading

## Contact and Support

For questions about API versioning:
- Documentation: https://docs.lexicraft.com/api/versioning
- Support: team@lexicraft.com
- Issues: https://github.com/lexicraft/practice-service/issues