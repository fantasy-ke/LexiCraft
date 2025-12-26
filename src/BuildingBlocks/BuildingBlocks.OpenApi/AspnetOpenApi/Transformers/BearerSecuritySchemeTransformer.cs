using System.Text.Json.Nodes;
using BuildingBlocks.Extensions.System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Transformers;

public class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.All(authScheme => authScheme.Name != "Bearer"))
        {
            return;
        }
        // 定义全局 Bearer 安全方案（注意接口类型）
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer".ToLowerInvariant(),
            In = ParameterLocation.Header,
            BearerFormat = "Json Web Token"
        };

        // 构造 SecurityRequirement，键为 OpenApiSecuritySchemeReference（新版本签名）
        var securitySchemeReference = new OpenApiSecuritySchemeReference("Bearer", document)
        {
            Reference = new JsonSchemaReference()
            {
                Id =  "Bearer",
                Type = ReferenceType.SecurityScheme
            },
        };

        var securityRequirement = new OpenApiSecurityRequirement
        {
            [securitySchemeReference] = []
        };

        // 给所有操作附加安全要求
        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null || path.Operations.Count == 0)
            {
                continue;
            }

            foreach (var operation in path.Operations.Values)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(securityRequirement);
            }
        }
    }
}
