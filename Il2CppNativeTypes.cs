using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime;

namespace MCG_AutoPlay;

internal static class Il2CppNativeTypes
{
    private static readonly string[] AssemblyNames = { "Assembly-CSharp.dll", "Assembly-CSharp" };
    private static readonly Dictionary<string, Type?> TypeCache = new();
    private static Assembly? _interopAssembly;

    private static Assembly? InteropAssembly
    {
        get
        {
            if (_interopAssembly != null)
                return _interopAssembly;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Assembly-CSharp")
                {
                    _interopAssembly = asm;
                    return _interopAssembly;
                }
            }

            return null;
        }
    }

    internal static bool IsInteropReady() => InteropAssembly != null;

    internal static Type? GetType(string className, string namespaze = "")
    {
        var cacheKey = string.IsNullOrEmpty(namespaze) ? className : namespaze + "." + className;
        if (TypeCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var asm = InteropAssembly;
        if (asm == null)
            return null;

        Type? resolved = ResolveManagedType(asm, className, namespaze);
        if (resolved == null)
            resolved = ResolveFromIl2CppPointer(asm, className, namespaze);

        if (resolved != null)
            TypeCache[cacheKey] = resolved;

        return resolved;
    }

    internal static MethodInfo? GetMethod(string className, string methodName, string namespaze = "")
    {
        var type = GetType(className, namespaze);
        if (type == null)
            return null;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        return type.GetMethod(methodName, flags);
    }

    internal static string DescribeMissing(params (string Type, string Namespace)[] types)
    {
        var missing = new List<string>();
        foreach (var (type, ns) in types)
        {
            if (GetType(type, ns) == null)
                missing.Add(string.IsNullOrEmpty(ns) ? type : ns + "." + type);
        }

        return missing.Count == 0 ? "interop not ready" : string.Join(", ", missing);
    }

    private static Type? ResolveManagedType(Assembly asm, string className, string namespaze)
    {
        foreach (var candidate in ManagedTypeCandidates(className, namespaze))
        {
            var resolved = asm.GetType(candidate, throwOnError: false);
            if (resolved != null)
                return resolved;
        }

        try
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.Name != className)
                    continue;

                if (string.IsNullOrEmpty(namespaze))
                    return type;

                if (type.Namespace == namespaze || type.FullName?.EndsWith($"{namespaze}.{className}") == true)
                    return type;
            }
        }
        catch
        {
        }

        return null;
    }

    private static Type? ResolveFromIl2CppPointer(Assembly asm, string className, string namespaze)
    {
        foreach (var ns in NamespaceCandidates(namespaze))
        {
            foreach (var assemblyName in AssemblyNames)
            {
                var classPtr = IL2CPP.GetIl2CppClass(assemblyName, ns, className);
                if (classPtr == IntPtr.Zero)
                    continue;

                try
                {
                    Il2CppType.TypeFromPointer(classPtr, BuildManagedName(className, ns));
                }
                catch
                {
                }

                var resolved = ResolveManagedType(asm, className, namespaze);
                if (resolved != null)
                    return resolved;
            }
        }

        return null;
    }

    private static IEnumerable<string> NamespaceCandidates(string namespaze)
    {
        if (!string.IsNullOrEmpty(namespaze))
            yield return namespaze;

        yield return "";
    }

    private static IEnumerable<string> ManagedTypeCandidates(string className, string namespaze)
    {
        if (!string.IsNullOrEmpty(namespaze))
        {
            yield return $"Il2Cpp.{namespaze}.{className}";
            yield return $"{namespaze}.{className}";
        }

        yield return $"Il2Cpp.{className}";
        yield return className;
    }

    private static string BuildManagedName(string className, string namespaze) =>
        string.IsNullOrEmpty(namespaze) ? className : namespaze + "." + className;
}
