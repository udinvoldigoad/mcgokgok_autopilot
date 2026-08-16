using System.Reflection;

namespace MCG_AutoPlay.Infrastructure;

/// <summary>
/// Membungkus Il2CppGameAccess dengan cache reflection sehingga resolusi
/// Type/Method/Field tidak diulang setiap tick.
///
/// Alur (PRD Phase 4):
///   Initialize -> Resolve Type -> Resolve Method -> Cache -> Game Loop -> Invoke cached
///
/// Cache dibuat thread-safe sederhana; semua operasi bersifat best-effort dan
/// tidak melempar exception ke pemanggil (disolasi via Il2CppGameAccess).
/// </summary>
internal static class ReflectionCache
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, MethodInfo?> MethodCache = new();
    private static readonly Dictionary<string, Type?> TypeCache = new();
    private static readonly Dictionary<string, PropertyInfo?> PropertyCache = new();
    private static readonly Dictionary<string, FieldInfo?> FieldCache = new();

    internal static Type? GetType(string typeName, string namespaze = "")
    {
        var key = string.IsNullOrEmpty(namespaze) ? typeName : namespaze + "." + typeName;
        lock (Gate)
        {
            if (TypeCache.TryGetValue(key, out var cached))
                return cached;
        }

        var resolved = Il2CppGameAccess.GetType(typeName, namespaze);
        lock (Gate)
        {
            if (!TypeCache.ContainsKey(key))
                TypeCache[key] = resolved;
            return resolved;
        }
    }

    internal static MethodInfo? GetMethod(string typeName, string methodName, string namespaze = "")
    {
        var type = GetType(typeName, namespaze);
        if (type == null)
            return null;

        var key = type.FullName + "::" + methodName;
        lock (Gate)
        {
            if (MethodCache.TryGetValue(key, out var cached))
                return cached;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var method = type.GetMethod(methodName, flags);
        lock (Gate)
        {
            if (!MethodCache.ContainsKey(key))
                MethodCache[key] = method;
            return method;
        }
    }

    internal static PropertyInfo? GetProperty(Type type, string memberName)
    {
        if (type == null)
            return null;

        var key = type.FullName + "::" + memberName;
        lock (Gate)
        {
            if (PropertyCache.TryGetValue(key, out var cached))
                return cached;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var prop = type.GetProperty(memberName, flags);
        lock (Gate)
        {
            if (!PropertyCache.ContainsKey(key))
                PropertyCache[key] = prop;
            return prop;
        }
    }

    internal static FieldInfo? GetField(Type type, string memberName)
    {
        if (type == null)
            return null;

        var key = type.FullName + "::" + memberName;
        lock (Gate)
        {
            if (FieldCache.TryGetValue(key, out var cached))
                return cached;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var field = type.GetField(memberName, flags);
        lock (Gate)
        {
            if (!FieldCache.ContainsKey(key))
                FieldCache[key] = field;
            return field;
        }
    }

    /// <summary>Invoke method via cache. Kembali null jika gagal / method tidak ada.</summary>
    internal static object? Invoke(object? instance, string methodName, params object?[] args)
    {
        if (instance == null)
            return null;

        var method = GetMethod(instance.GetType().Name, methodName, instance.GetType().Namespace ?? "");
        if (method == null)
            return null;

        try
        {
            return method.Invoke(instance, args);
        }
        catch
        {
            return null;
        }
    }

    internal static object? GetMemberValue(object? instance, string memberName)
    {
        if (instance == null)
            return null;

        var type = instance.GetType();

        var prop = GetProperty(type, memberName);
        if (prop != null)
        {
            try
            {
                return prop.GetValue(instance);
            }
            catch
            {
                // fall through ke field
            }
        }

        var field = GetField(type, memberName);
        try
        {
            return field?.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }
}
