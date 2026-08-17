using System.Reflection;

namespace MCG_AutoPlay;

/// <summary>Stub minimal Il2CppGameAccess agar Infrastructure/ & Game/ bisa di-compile & diuji tanpa MelonLoader.</summary>
internal static class Il2CppGameAccess
{
    internal static Type? GetType(string typeName, string namespaze = "") => Type.GetType(typeName) ?? null;

    internal static object? GetSingleton(string typeName, string namespaze = "")
    {
        var type = Type.GetType(typeName);
        if (type == null)
            return null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        return type.GetProperty("Instance", flags)?.GetValue(null)
            ?? type.GetField("Instance", flags)?.GetValue(null);
    }

    internal static object? Invoke(object? instance, string methodName, params object?[] args)
    {
        if (instance == null)
            return null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        return instance.GetType().GetMethod(methodName, flags)?.Invoke(instance, args);
    }

    internal static object? GetMemberValue(object? instance, string memberName)
    {
        if (instance == null)
            return null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return instance.GetType().GetProperty(memberName, flags)?.GetValue(instance)
            ?? instance.GetType().GetField(memberName, flags)?.GetValue(instance);
    }

    internal static T? GetMemberValue<T>(object? instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        if (value == null)
            return default;
        if (value is T typed)
            return typed;
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    internal static bool GetBoolProperty(object? instance, string propertyName)
    {
        if (instance == null)
            return false;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        if (instance.GetType().GetProperty(propertyName, flags)?.GetValue(instance) is bool b)
            return b;
        if (instance.GetType().GetMethod(propertyName, flags)?.Invoke(instance, null) is bool b2)
            return b2;
        return false;
    }

    internal static object? GetListItem(object? list, int index)
    {
        if (list is System.Collections.IList il)
        {
            if (index < 0 || index >= il.Count)
                return null;
            return il[index];
        }
        return null;
    }
}
