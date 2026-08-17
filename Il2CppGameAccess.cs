using System.Collections;
using System.Reflection;
using Il2CppInterop.Runtime;

namespace MCG_AutoPlay;

internal static class Il2CppGameAccess
{
    private const BindingFlags InstanceAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags StaticAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    internal static Type? GetType(string typeName, string namespaze = "") =>
        Il2CppNativeTypes.GetType(typeName, namespaze);

    internal static object? GetSingleton(string typeName, string namespaze = "")
    {
        foreach (var member in new[] { "Instance", "_instance", "_inst" })
        {
            var obj = GetStaticMember(typeName, member, namespaze);
            if (obj != null)
                return obj;
        }

        return null;
    }

    internal static object? GetStaticMember(string typeName, string memberName, string namespaze = "")
    {
        try
        {
            var type = GetType(typeName, namespaze);
            if (type == null)
                return null;

            var prop = type.GetProperty(memberName, StaticAny);
            if (prop != null)
                return prop.GetValue(null);

            var field = type.GetField(memberName, StaticAny);
            if (field != null)
                return field.GetValue(null);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
        }

        return null;
    }

    internal static object? Invoke(object? instance, string methodName, params object?[] args)
    {
        if (instance == null)
            return null;

        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var method = instance.GetType().GetMethod(methodName, flags);
            return method?.Invoke(instance, args);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
            return null;
        }
    }

    internal static object? InvokeStatic(string typeName, string methodName, params object?[] args)
    {
        var type = GetType(typeName);
        if (type == null)
            return null;

        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var method = type.GetMethod(methodName, flags);
            return method?.Invoke(null, args);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
            return null;
        }
    }

    internal static object? GetMemberValue(object? instance, string memberName)
    {
        if (instance == null)
            return null;

        try
        {
            var type = instance.GetType();
            var prop = type.GetProperty(memberName, InstanceAny);
            if (prop != null)
                return prop.GetValue(instance);

            var field = type.GetField(memberName, InstanceAny);
            if (field != null)
                return field.GetValue(instance);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
        }

        return null;
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

    internal static void SetMemberValue(object instance, string memberName, object? value)
    {
        try
        {
            var type = instance.GetType();
            var prop = type.GetProperty(memberName, InstanceAny);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(instance, value);
                return;
            }

            var field = type.GetField(memberName, InstanceAny);
            field?.SetValue(instance, value);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
        }
    }

    internal static void SetStaticMember(string typeName, string memberName, object? value, string namespaze = "")
    {
        try
        {
            var type = GetType(typeName, namespaze);
            if (type == null)
                return;

            var prop = type.GetProperty(memberName, StaticAny);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, value);
                return;
            }

            var field = type.GetField(memberName, StaticAny);
            field?.SetValue(null, value);
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
        }
    }

    internal static bool TryDictionaryGet(object? dictionary, ulong key, out object? value)
    {
        value = null;
        if (dictionary == null)
            return false;

        try
        {
            var tryGet = dictionary.GetType().GetMethod("TryGetValue");
            if (tryGet == null)
                return false;

            var args = new object?[] { key, null };
            if (tryGet.Invoke(dictionary, args) is not true)
                return false;

            value = args[1];
            return value != null;
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
            return false;
        }
    }

    internal static object? GetListItem(object? list, int index)
    {
        if (list is IList il)
        {
            if (index < 0 || index >= il.Count)
                return null;

            return il[index];
        }

        try
        {
            var count = Convert.ToInt32(list?.GetType().GetProperty("Count")?.GetValue(list) ?? 0);
            if (index < 0 || index >= count)
                return null;

            return list?.GetType().GetProperty("Item")?.GetValue(list, new object[] { index });
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
            return null;
        }
    }

    internal static ulong GetStaticUInt64(string typeName, string memberName, string namespaze = "")
    {
        var value = GetStaticMember(typeName, memberName, namespaze);
        if (value == null)
            return 0;

        try
        {
            return Convert.ToUInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    internal static ulong GetUInt64(object? instance, string memberName)
    {
        return GetMemberValue<ulong>(instance, memberName);
    }

    internal static bool GetBoolProperty(object? instance, string propertyName)
    {
        if (instance == null)
            return false;

        try
        {
            var prop = instance.GetType().GetProperty(propertyName, InstanceAny);
            if (prop?.GetValue(instance) is bool b)
                return b;
        }
        catch (Exception ex) when (IsIl2CppAccessFailure(ex))
        {
        }

        return false;
    }

    private static bool IsIl2CppAccessFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is Il2CppException or TargetInvocationException)
                return true;
        }

        return false;
    }
}