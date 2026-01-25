using System.ComponentModel;

namespace BuildingBlocks.Extensions.System;

public static class EnumExtensions
{
    public static string GetDescription(this Enum val)
    {
        var field = val.GetType().GetField(val.ToString());
        if (field == null) return val.ToString();
        var customAttribute = Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
        return customAttribute == null ? val.ToString() : ((DescriptionAttribute)customAttribute).Description;
    }
}