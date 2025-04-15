using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ZService.Analyzers.Extensions;

namespace ZService.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class FantasyGenerator : IIncrementalGenerator
    {
        private const string FantasyApiBaseTypeName = "FantasyApi";
        private const string ServiceSuffix = "Service";
        private const string AsyncSuffix = "Async";

        private ImmutableArray<ClassInfo> GetReferencedClasses(Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<ClassInfo>();

            foreach (var reference in compilation.References)
            {
                var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                if (symbol is not IAssemblySymbol assemblySymbol)
                    continue;
                
                // 过滤掉微软的 System 程序集
                if (assemblySymbol.Name.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                    assemblySymbol.Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var type in assemblySymbol.GlobalNamespace.GetNamespaceTypes())
                {
                    // 检查类型是否继承自 FantasyService
                    if (InheritsFromFantasyService(type))
                    {
                        var classInfo = GetClassInfo(type);
                        if (classInfo != null)
                        {
                            builder.Add(classInfo);
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        private ClassInfo? GetClassInfo(INamedTypeSymbol classSymbol)
        {
            var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);
            var className = classSymbol.Name;
            var attributes = classSymbol.GetAttributes();
            
            // 获取公共非静态方法
            var methods = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && 
                            !m.IsStatic &&
                            m.MethodKind == MethodKind.Ordinary)
                .ToList();

            // 获取 RouteAttribute
            var routeAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "RouteAttribute");
            var route = routeAttr?.ConstructorArguments.FirstOrDefault().Value as string ??
                      $"/api/{className.TrimEnd(ServiceSuffix)}";
            // 获取 Tags 属性
            var tagsAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "TagsAttribute");
            var tags = tagsAttr?.ConstructorArguments.FirstOrDefault().Values.FirstOrDefault();

            // 获取 FilterAttributes
            var filterAttributes = attributes.Where(a =>
                a.AttributeClass?.Name == "FilterAttribute" || a.AttributeClass?.Name == "Filter").ToList();

            // 获取 AuthorizeAttributes
            var authorizeAttributes = attributes.Where(a => 
                a.AttributeClass?.Name?.StartsWith("Authorize") == true).ToList();

            return new ClassInfo
            {
                Namespace = namespaceName,
                ClassName = className,
                Route = route,
                Tags = tags.Value.Value.ToString(),
                FilterAttributes = filterAttributes,
                AuthorizeAttributes = authorizeAttributes,
                Methods = methods
            };
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 获取编译对象
            var compilationProvider = context.CompilationProvider;

            // 收集当前项目的类信息
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsCandidateClass,
                    transform: GetSemanticTargetForGeneration)
                .Where(m => m != null)
                .Collect();

            // 注册源输出
            context.RegisterSourceOutput(
                compilationProvider.Combine(classDeclarations),
                (spc, source) =>
                {
                    var (compilation, classes) = source;

                    // 获取引用的程序集中的类型
                    var referencedClasses = GetReferencedClasses(compilation);

                    // 将当前项目和引用程序集的类合并
                    var allClasses = classes.AddRange(referencedClasses);

                    GenerateSource(spc, compilation, allClasses);
                });
        }

        private static bool IsCandidateClass(SyntaxNode node, CancellationToken _)
        {
            return node is ClassDeclarationSyntax { BaseList: not null };
        }

        private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var model = context.SemanticModel;

            // 获取类的符号信息
            if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                return null;

            // 检查类是否继承自 FantasyService
            if (!InheritsFromFantasyService(classSymbol))
                return null;

            // 获取命名空间
            var namespaceName = GetFullNamespace(classSymbol.ContainingNamespace);

            // 获取类名
            var className = classSymbol.Name;

            // 获取类级别的特性
            var attributes = classSymbol.GetAttributes();

            // 获取 RouteAttribute
            var routeAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "RouteAttribute");
            var route = routeAttr?.ConstructorArguments.FirstOrDefault().Value as string ??
                        $"/api/{className.TrimEnd(ServiceSuffix)}";
                        
            // 获取 Tags 属性
            var tagsAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "Tags");
            var tags = tagsAttr?.ConstructorArguments.FirstOrDefault().Value as string;

            // 获取 FilterAttributes
            var filterAttributes = attributes.Where(a =>
                a.AttributeClass?.Name == "FilterAttribute" || a.AttributeClass?.Name == "Filter").ToList();

            var authorizeAttributes = attributes.Where(a => 
                a.AttributeClass?.Name?.StartsWith("Authorize") == true).ToList();

            var zAuthorizeAttributes = attributes.Where(a => 
                a.AttributeClass?.Name?.StartsWith("ZAuthorize") == true).ToList();
            
            // 获取所有公共非静态方法
            var methods = classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && 
                           !m.IsStatic &&
                           m.MethodKind == MethodKind.Ordinary)
                .ToList();

            return new ClassInfo
            {
                Namespace = namespaceName,
                ClassName = className,
                Route = route,
                AuthorizeAttributes = authorizeAttributes,
                ZAuthorizeAttributes = zAuthorizeAttributes,
                Tags = tags,
                FilterAttributes = filterAttributes,
                Methods = methods
            };
        }

        private static bool InheritsFromFantasyService(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == FantasyApiBaseTypeName)
                    return true;
                baseType = baseType.BaseType;
            }

            return false;
        }

        private static string GetFullNamespace(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                return string.Empty;

            var parts = new Stack<string>();
            var current = namespaceSymbol;
            while (current is { IsGlobalNamespace: false })
            {
                parts.Push(current.Name);
                current = current.ContainingNamespace;
            }

            return string.Join(".", parts);
        }

        private void GenerateSource(SourceProductionContext context, Compilation compilation,
            ImmutableArray<ClassInfo?> classes)
        {
            var classInfos = classes.Where(c => c != null).Select(c => c!).ToList();

            if (!classInfos.Any())
                return;

            // 生成 DI 注册代码
            var diRegistration = GenerateDiRegistration(classInfos);

            // 生成 API 映射代码
            var apiMappings = GenerateApiMappings(classInfos, compilation, context.CancellationToken);

            // 构建最终的源代码
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// 这是由ZService.Analyzers自动生成的");
            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Builder;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Authorization;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    public static class FantasyExtensions");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine(
                "        public static IServiceCollection WithFantasyLife(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine(diRegistration);
            sourceBuilder.AppendLine("            return services;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        public static WebApplication MapFantasyApi(this WebApplication app)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine(apiMappings);
            sourceBuilder.AppendLine("            return app;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // 添加生成的源代码
            context.AddSource("FantasyExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static string GenerateDiRegistration(List<ClassInfo> classInfos)
        {
            var sb = new StringBuilder();
            
            // 将类信息按命名空间分组
            var classInfosByNamespace = classInfos.GroupBy(c => c.Namespace);
            
            sb.AppendLine("            switch (lifetime)");
            sb.AppendLine("            {");
            sb.AppendLine("                case ServiceLifetime.Singleton:");
            var infosByNamespace = classInfosByNamespace as IGrouping<string, ClassInfo>[] ?? classInfosByNamespace.ToArray();
            foreach (var group in infosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }
                
                foreach (var classInfo in group)
                {
                    sb.AppendLine($"                    services.AddSingleton<{classInfo.Namespace}.{classInfo.ClassName}>();");
                }
            }
            sb.AppendLine("                    break;");
            
            sb.AppendLine("                case ServiceLifetime.Scoped:");
            foreach (var group in infosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }
                
                foreach (var classInfo in group)
                {
                    sb.AppendLine($"                    services.AddScoped<{classInfo.Namespace}.{classInfo.ClassName}>();");
                }
            }
            sb.AppendLine("                    break;");
            
            sb.AppendLine("                case ServiceLifetime.Transient:");
            foreach (var group in infosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"                    // {group.Key}");
                }
                
                foreach (var classInfo in group)
                {
                    sb.AppendLine($"                    services.AddTransient<{classInfo.Namespace}.{classInfo.ClassName}>();");
                }
            }
            sb.AppendLine("                    break;");
            
            sb.AppendLine("            }");

            return sb.ToString();
        }

        private string GenerateApiMappings(List<ClassInfo> classInfos, Compilation compilation,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            
            // 按命名空间分组
            var classInfosByNamespace = classInfos.GroupBy(c => c.Namespace);
            
            foreach (var group in classInfosByNamespace)
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    sb.AppendLine($"            // {group.Key}");
                }
                
                foreach (var classInfo in group)
                {
                    var instanceName = char.ToLowerInvariant(classInfo.ClassName[0]) +
                                     classInfo.ClassName.TrimEnd(ServiceSuffix);

                    sb.AppendLine(
                        $"            var {instanceName} = app.MapGroup(\"{classInfo.Route}\"){GenerateClassAttributes(classInfo)};");

                    foreach (var methodCode in classInfo.Methods.Select(method => GenerateMethodMapping(method, classInfo, compilation, instanceName, cancellationToken)).Where(methodCode => !string.IsNullOrWhiteSpace(methodCode)))
                    {
                        sb.AppendLine(methodCode);
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private string GenerateClassAttributes(ClassInfo classInfo)
        {
            var sb = new StringBuilder();

            // 添加类级别的过滤器
            foreach (var filter in classInfo.FilterAttributes.Where(filterAttr => filterAttr.ConstructorArguments.Length > 0).Select(filterAttr => filterAttr.ConstructorArguments.FirstOrDefault().Values).SelectMany(filterTypes => filterTypes))
            {
                if (filter.Value is INamedTypeSymbol filterType)
                {
                    sb.AppendLine($".AddEndpointFilter<{filterType.ToDisplayString()}>()");
                }
            }
            
            // 处理授权属性
            if (classInfo.AuthorizeAttributes.Any())
            {
                // 获取 AuthorizeAttribute 的 Roles 属性
                var rolesArg = classInfo.AuthorizeAttributes
                    .SelectMany(a => a.NamedArguments)
                    .FirstOrDefault(n => n.Key == "Roles");
                
                // 获取 AuthorizeAttribute 的 Policy 属性
                var policyArg = classInfo.AuthorizeAttributes
                    .SelectMany(a => a.NamedArguments)
                    .FirstOrDefault(n => n.Key == "Policy");

                if (!rolesArg.Equals(default) && !policyArg.Equals(default) && 
                    rolesArg.Value.Value is string roles && policyArg.Value.Value is string policy)
                {
                    sb.AppendLine($".RequireAuthorization(p => p.RequireRole(\"{roles}\").RequirePolicy(\"{policy}\"))");
                }
                else if (!rolesArg.Equals(default) && rolesArg.Value.Value is string role)
                {
                    sb.AppendLine($".RequireAuthorization(p => p.RequireRole(\"{role}\"))");
                }
                else if (!policyArg.Equals(default) && policyArg.Value.Value is string policyValue)
                {
                    sb.AppendLine($".RequireAuthorization(\"{policyValue}\")");
                }
                else
                {
                    sb.AppendLine(".RequireAuthorization()");
                }
            }

            if (classInfo.ZAuthorizeAttributes.Any())
            {
                // 获取 ZAuthorizeAttribute 的 AuthorizeName 属性
                var policyArg = classInfo.ZAuthorizeAttributes
                    .SelectMany(a => a.NamedArguments)
                    .FirstOrDefault(n => n.Key == "AuthorizeName");

                if (!policyArg.Equals(default) && policyArg.Value.Value is string[] policyArr)
                {
                    sb.AppendLine($".RequireAuthorization(\"{string.Join(",", policyArr)}\")");
                }
                else
                {
                    sb.AppendLine(".RequireAuthorization()");
                }
            }
            // 添加标签
            if (!string.IsNullOrEmpty(classInfo.Tags))
            {
                sb.AppendLine($".WithTags(\"{classInfo.Tags}\")");
            }

            return sb.ToString();
        }

        private string GenerateMethodMapping(IMethodSymbol method, ClassInfo classInfo, Compilation compilation,
            string instanceName, CancellationToken cancellationToken)
        {
            // 跳过具有 IgnoreRouteAttribute 的方法
            if (method.GetAttributes().Any(a => a.AttributeClass?.Name == "IgnoreRouteAttribute"))
                return string.Empty;

            // 确定 HTTP 方法和路由
            var (httpMethod, route) = DetermineHttpMethodAndRoute(method);

            // 获取方法级别的属性，排除 FilterAttribute
            var methodAttributes =
                method.GetAttributes().Where(a => a.AttributeClass?.Name != "FilterAttribute").ToList();

            // 构建属性字符串
            var attributesString = string.Join("\n", methodAttributes.Select(a => $"                [{a}]"));

            if (!string.IsNullOrEmpty(attributesString))
            {
                attributesString = "\n" + attributesString;
            }

            // 获取方法级别的过滤器
            var filterAttributes =
                method.GetAttributes().Where(a => a.AttributeClass?.Name == "FilterAttribute").ToList();
                
            var filterExtensions = new StringBuilder();
            foreach (var filter in filterAttributes.Where(filterAttr => filterAttr.ConstructorArguments.Length > 0).Select(filterAttr => filterAttr.ConstructorArguments[0].Values).SelectMany(filterTypes => filterTypes))
            {
                if (filter.Value is INamedTypeSymbol filterType)
                {
                    filterExtensions.AppendLine(
                        $"                .AddEndpointFilter<{filterType.ToDisplayString()}>()");
                }
            }
            
            // 获取方法级别的权限
            var zAuthExtensions = new StringBuilder();
            var zAuthorizeAttributes =
                method.GetAttributes().Where(a => a.AttributeClass?.Name == "ZAuthorizeAttribute").ToList();
            
            if (zAuthorizeAttributes.Any())
            {
                foreach (var zAuthArrName in from zAuth in zAuthorizeAttributes where zAuth.ConstructorArguments.Length > 0 select zAuth.NamedArguments.FirstOrDefault(n => n.Key == "AuthorizeName"))
                {
                    ;
                    if (!zAuthArrName.Equals(default) && zAuthArrName.Value.Value is string[] zAuthArr)
                    {
                        zAuthExtensions.AppendLine($".RequireAuthorization(\"{string.Join(",", zAuthArr)}\")");
                    }
                    else
                    {
                        zAuthExtensions.AppendLine(".RequireAuthorization()");
                    }
                }
            }
            

            // 获取方法参数
            var parameters = method.Parameters;
            var parameterList = string.Join(", ", parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
            var parameterNames = string.Join(", ", parameters.Select(p => p.Name));

            // 确定服务实例名称
            var serviceInstance = char.ToLowerInvariant('_') + RemoveServiceSuffixAndLowercaseFirstLetter(classInfo.ClassName);

            // 构建 Lambda 表达式
            string lambda;
            if (parameters.Length > 0)
            {
                lambda =
                    $"async ({classInfo.Namespace}.{classInfo.ClassName} {serviceInstance}, {parameterList}) => await {serviceInstance}.{method.Name}({parameterNames})";
            }
            else
            {
                lambda =
                    $"async ({classInfo.Namespace}.{classInfo.ClassName} {serviceInstance}) => await {serviceInstance}.{method.Name}()";
            }
            
            

            // 组合所有部分生成方法代码
            var methodCode = $@"            {instanceName}.Map{httpMethod}(""{route}"",{attributesString}
                {lambda}){filterExtensions};";

            return methodCode;
        }

        private static string RemoveServiceSuffixAndLowercaseFirstLetter(string input)
        {
            // 1. 截断末尾的 "Service"
            if (input.EndsWith(ServiceSuffix))
            {
                input = input.TrimEnd(ServiceSuffix);
            }

            // 2. 确保字符串非空，处理首字母小写
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToLower(input[0]) + input.Substring(1);
        }

        private (string httpMethod, string route) DetermineHttpMethodAndRoute(IMethodSymbol method)
        {
            // 检查是否有 HTTP 方法特性
            var httpMethodAttr = method.GetAttributes().FirstOrDefault(a => a.AttributeClass != null &&
                                                                           (a.AttributeClass.Name == "HttpGetAttribute" ||
                                                                            a.AttributeClass.Name == "HttpPostAttribute" ||
                                                                            a.AttributeClass.Name == "HttpPutAttribute" ||
                                                                            a.AttributeClass.Name == "HttpDeleteAttribute"));

            if (httpMethodAttr is { AttributeClass: not null })
            {
                var attrName = httpMethodAttr.AttributeClass.Name;
                string httpMethod = attrName switch
                {
                    "HttpGetAttribute" => "Get",
                    "HttpPostAttribute" => "Post",
                    "HttpPutAttribute" => "Put",
                    "HttpDeleteAttribute" => "Delete",
                    _ => "Post"
                };

                // 获取路由参数，如果有
                var route = httpMethodAttr.ConstructorArguments.FirstOrDefault().Value as string ?? method.Name;

                return (httpMethod, route);
            }

            // 根据方法名推断 HTTP 方法和路由
            var methodName = method.Name;
            string inferredHttpMethod;
            string inferredRoute;

            if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Get";
                inferredRoute = methodName.Substring(3);
            }
            else if (methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Delete";
                inferredRoute = methodName.Substring(6);
            }
            else if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Delete";
                inferredRoute = methodName.Substring(6);
            }
            else if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Post";
                inferredRoute = methodName.Substring(4);
            }
            else if (methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Post";
                inferredRoute = methodName.Substring(6);
            }
            else if (methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Post";
                inferredRoute = methodName.Substring(3);
            }
            else if (methodName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Post";
                inferredRoute = methodName.Substring(6);
            }
            else if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Put";
                inferredRoute = methodName.Substring(3);
            }
            else if (methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Put";
                inferredRoute = methodName.Substring(6);
            }
            else if (methodName.StartsWith("Modify", StringComparison.OrdinalIgnoreCase))
            {
                inferredHttpMethod = "Put";
                inferredRoute = methodName.Substring(6);
            }
            else
            {
                inferredHttpMethod = "Post";
                inferredRoute = methodName;
            }

            // 移除 'Async' 后缀
            if (inferredRoute.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                inferredRoute = inferredRoute.Substring(0, inferredRoute.Length - AsyncSuffix.Length);
            }

            // 移除 'Service' 后缀
            if (inferredRoute.EndsWith(ServiceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                inferredRoute = inferredRoute.Substring(0, inferredRoute.Length - ServiceSuffix.Length);
            }

            // 确保路由以 '/' 开头
            if (!inferredRoute.StartsWith("/"))
                inferredRoute = "/" + inferredRoute;

            return (inferredHttpMethod, inferredRoute);
        }

        private class ClassInfo
        {
            public string Namespace { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public string? Tags { get; set; }
            public List<AttributeData> AuthorizeAttributes { get; set; } = [];
            
            public List<AttributeData> ZAuthorizeAttributes { get; set; } = [];
            public List<AttributeData> FilterAttributes { get; set; } = [];
            public List<IMethodSymbol> Methods { get; set; } = [];
        }
    }
}