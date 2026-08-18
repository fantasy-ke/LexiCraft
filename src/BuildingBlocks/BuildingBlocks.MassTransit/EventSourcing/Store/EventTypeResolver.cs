using System.Reflection;

namespace BuildingBlocks.MassTransit.EventSourcing.Store;

/// <summary>
///     把事件存储中持久化的程序集限定类型名解析回当前进程中的 CLR 类型。
/// </summary>
/// <remarks>
///     解析器用于兼容历史记录：当事件被写入时所用的程序集或命名空间首段之后发生了重命名，
///     精确解析会失败，此时按“去掉首段后的程序集名与类型全名”在注册的事件程序集内做唯一匹配。
///     解析器不包含任何具体的历史名称字面量，因此不会随品牌变化而失效。
/// </remarks>
public interface IEventTypeResolver
{
    /// <summary>
    ///     解析持久化的类型名。
    /// </summary>
    /// <param name="assemblyQualifiedTypeName">写入事件时记录的 <see cref="Type.AssemblyQualifiedName" />。</param>
    /// <returns>命中唯一类型、命中多个候选或无命中的解析结果。</returns>
    EventTypeResolution Resolve(string assemblyQualifiedTypeName);
}

/// <summary>
///     事件类型解析结果。
/// </summary>
/// <param name="Type">解析出的类型；无法解析时为 <see langword="null" />。</param>
/// <param name="IsAmbiguous">是否因为命中多个候选类型而拒绝解析。</param>
public readonly record struct EventTypeResolution(Type? Type, bool IsAmbiguous = false);

/// <summary>
///     <see cref="IEventTypeResolver" /> 的默认实现，只在调用方显式注册的事件程序集范围内查找候选类型。
/// </summary>
/// <param name="eventAssemblies">注册消费者与事件时传入的程序集集合；动态程序集会被忽略。</param>
public sealed class EventTypeResolver(IEnumerable<Assembly> eventAssemblies) : IEventTypeResolver
{
    private readonly Assembly[] _eventAssemblies = eventAssemblies
        .Where(assembly => !assembly.IsDynamic)
        .Distinct()
        .ToArray();

    /// <inheritdoc />
    public EventTypeResolution Resolve(string assemblyQualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
            return default;

        var exactType = Type.GetType(assemblyQualifiedTypeName, throwOnError: false);
        if (exactType != null)
            return new EventTypeResolution(exactType);

        if (!TrySplitTypeName(assemblyQualifiedTypeName, out var storedTypeName, out var storedAssemblyName))
            return default;

        var typeSuffix = RemoveBrandSegment(storedTypeName);
        var assemblySuffix = RemoveBrandSegment(storedAssemblyName);
        var candidates = _eventAssemblies
            .Where(assembly => string.Equals(
                RemoveBrandSegment(assembly.GetName().Name ?? string.Empty),
                assemblySuffix,
                StringComparison.Ordinal))
            .SelectMany(GetLoadableTypes)
            .Where(type => string.Equals(
                RemoveBrandSegment(type.FullName ?? string.Empty),
                typeSuffix,
                StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        return candidates.Length switch
        {
            1 => new EventTypeResolution(candidates[0]),
            > 1 => new EventTypeResolution(null, IsAmbiguous: true),
            _ => default
        };
    }

    private static bool TrySplitTypeName(
        string assemblyQualifiedTypeName,
        out string typeName,
        out string assemblyName)
    {
        var separatorIndex = assemblyQualifiedTypeName.IndexOf(',', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            typeName = string.Empty;
            assemblyName = string.Empty;
            return false;
        }

        typeName = assemblyQualifiedTypeName[..separatorIndex].Trim();
        var assemblyPart = assemblyQualifiedTypeName[(separatorIndex + 1)..].Trim();
        var assemblySeparatorIndex = assemblyPart.IndexOf(',', StringComparison.Ordinal);
        assemblyName = (assemblySeparatorIndex < 0 ? assemblyPart : assemblyPart[..assemblySeparatorIndex]).Trim();
        return typeName.Length > 0 && assemblyName.Length > 0;
    }

    private static string RemoveBrandSegment(string name)
    {
        var separatorIndex = name.IndexOf('.', StringComparison.Ordinal);
        return separatorIndex < 0 ? name : name[(separatorIndex + 1)..];
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
