using System.Text.Json.Nodes;
using BuildingBlocks.Extensions.System;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Transformers;

public class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        //找出枚举类型
        if (context.JsonTypeInfo.Type.BaseType != typeof(Enum)) return Task.CompletedTask;
        var list = new List<JsonNode>();
        //获取枚举项
        foreach (var enumValue in schema.Enum?.OfType<JsonNode>() ?? Enumerable.Empty<JsonNode>())
            //把枚举项转为枚举类型
            if (Enum.TryParse(context.JsonTypeInfo.Type, enumValue.GetValue<string>(), out var result))
            {
                //通过枚举扩展方法获取枚举描述
                var description = ((Enum)result).GetDescription();
                //重新组织枚举值展示结构（作为字符串 Json 节点加入）
                list.Add(JsonValue.Create($"{enumValue.GetValue<string>()} - {description}"));
            }
            else
            {
                list.Add(enumValue);
            }

        schema.Enum = list;

        return Task.CompletedTask;
    }
}