using System;
using System.Linq;
using System.Reflection;

partial class CodeBridge
{
    // Reflection helpers shared by feature bridges. Axure's merged/obfuscated
    // assembly exposes many members on base types, so always walk the chain.
    static MethodInfo FindMethod(Type type, string name, Type[] parameterTypes = null)
    {
        for (Type t = type; t != null; t = t.BaseType)
        {
            var method = parameterTypes == null
                ? t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                : t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,
                              null, parameterTypes, null);
            if (method != null) return method;
        }
        throw new MissingMethodException(type.FullName, name);
    }

    static PropertyInfo FindProperty(Type type, string name)
    {
        for (Type t = type; t != null; t = t.BaseType)
        {
            var property = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property != null) return property;
        }
        throw new MissingMemberException(type.FullName, name);
    }

    static object GetProperty(object target, string name)
        => FindProperty(target.GetType(), name).GetValue(target);

    static void SetProperty(object target, string name, object value)
        => FindProperty(target.GetType(), name).SetValue(target, value);

    static object Invoke(object target, string name, params object[] args)
        => FindMethod(target.GetType(), name).Invoke(target, args);

    static object Invoke(Type type, string name, params object[] args)
        => FindMethod(type, name).Invoke(null, args);

    static object New(string typeName, params object[] args)
        => Activator.CreateInstance(T(typeName), args);
}
