using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Vocabulary.Words.Features.ImportWords;

public static class ImportWordsEndpoint
{
    internal static RouteHandlerBuilder MapImportWordsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("word-lists/import", Handle)
            .RequireAuthorization(VocabularyPermissions.Words.Import)
            .WithName(nameof(ImportWords))
            .WithDisplayName(nameof(ImportWords).Humanize())
            .WithSummary("导入词库JSON数据".Humanize())
            .WithDescription("根据上传的JSON内容创建词库并导入单词".Humanize())
            .DisableAntiforgery(); // Ensure upload works if hitting CSRF

        async Task<WordImportResult> Handle(
            [AsParameters] ImportWordsRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(new ImportWordsCommand(request), cancellationToken);
            
            return result;
        }
    }
}

internal record ImportWordsRequestParameters(
    IMediator Mediator,
    [FromBody] ImportWordsRequest Request,
    CancellationToken CancellationToken
);
