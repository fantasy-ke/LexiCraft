// using LexiCraft.Infrastructure.Model;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.HttpResults;
//
// namespace LexiCraft.Infrastructure.Filters;
//
// public class ResultEndPointFilter1 : IEndpointFilter
// {
//     public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
//     {
//         var result = await next(context);
//
//         return result is not FileStreamHttpResult ? ResultDto.Sucess(result) : result;
//     }
// }