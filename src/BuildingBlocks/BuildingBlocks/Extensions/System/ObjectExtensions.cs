namespace BuildingBlocks.Extensions.System;

public static class ObjectExtensions
{
    public static T As<T>(this object obj) where T : class
    {
        return (T)obj;
    }

    public static bool ObjToBool(this object? thisValue)
    {
        bool reval = thisValue != null && thisValue != DBNull.Value && bool.TryParse(thisValue.ToString(), out reval);

        return reval;
    }

    /// <summary>
    ///     把对象类型转换为指定类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="conversionType"></param>
    /// <returns></returns>
    public static object? CastTo(this object? value, Type conversionType)
    {
        if (value == null) return null;

        if (conversionType == typeof(string)) return value?.ToString();

        if (conversionType == typeof(int) || conversionType == typeof(decimal) ||
            conversionType == typeof(float) || conversionType == typeof(double))
        {
            if ((value.ToString() ?? "").Trim().Length > 0)
                if (double.TryParse(value.NotNullString().Trim(), out var result))
                    return Convert.ChangeType(result, conversionType);

            return 0;
        }

        if (conversionType.IsNullableType())
        {
            if (value.NotNullString().Length == 0) return null;

            conversionType = conversionType.GetUnNullableType();
        }
        else if (conversionType.IsEnum)
        {
            return Enum.Parse(conversionType, value.ToString() ?? "", true);
        }
        else if (conversionType == typeof(Guid))
        {
            return Guid.Parse(value.ToString() ?? "");
        }

        return Convert.ChangeType(value.NotNullString().Trim(), conversionType);
    }

    /// <summary>
    ///     把对象类型转化为指定类型，转化失败时返回指定的默认值
    /// </summary>
    /// <typeparam name="T"> 动态类型 </typeparam>
    /// <param name="value"> 要转化的源对象 </param>
    /// <param name="defaultValue"> 转化失败返回的指定默认值 </param>
    /// <returns> 转化后的指定类型对象，转化失败时返回指定的默认值 </returns>
    public static T? CastTo<T>(this object? value, T? defaultValue)
    {
        try
        {
            switch (value)
            {
                case null:
                    return defaultValue ?? default;
                case string:
                    return (T?)value.CastTo(typeof(T)) ?? default;
                default:
                {
                    if (value.GetType() == typeof(T)) return (T)value;

                    break;
                }
            }

            var result = value.CastTo(typeof(T));
            return (T?)result ?? default;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    /// <summary>
    ///     把对象类型转化为指定类型
    /// </summary>
    /// <typeparam name="T"> 动态类型 </typeparam>
    /// <param name="value"> 要转化的源对象 </param>
    /// <returns> 转化后的指定类型的对象，转化失败引发异常。 </returns>
    public static T CastTo<T>(this object value)
    {
        return value.CastTo<T>(default!)!;
    }
}