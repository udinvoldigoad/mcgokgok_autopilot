using System.Reflection;

namespace MCG_AutoPlay;

/// <summary>Stub minimal Il2CppGameAccess agar Infrastructure/ & Game/ bisa di-compile & diuji tanpa MelonLoader.</summary>
internal static class Il2CppGameAccess
{
    internal static Type? GetType(string typeName, string namespaze = "") => Type.GetType(typeName) ?? null;

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
}
