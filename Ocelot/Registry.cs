using System.Reflection;

namespace Ocelot;

public static class Registry
{
    private readonly static HashSet<Assembly> RegisteredAssemblies = [];

    private readonly static List<Type> CachedTypes = [];

    public static void RegisterAssemblies(params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            if (RegisteredAssemblies.Add(asm))
            {
                try
                {
                    Type[] types;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types.Where(t => t != null).ToArray()!;
                    }

                    CachedTypes.AddRange(types);
                }
                catch
                {
                }
            }
        }
    }

    public static IEnumerable<Type> GetAllLoadableTypes()
    {
        return CachedTypes;
    }

    public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        return GetAllLoadableTypes().Where(t => !t.IsAbstract && t.IsDefined(typeof(TAttribute), false));
    }

    public static IEnumerable<Type> GetInterfaceTypesWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        return GetAllLoadableTypes().Where(t => t.IsInterface && t.IsDefined(typeof(TAttribute), false));
    }

    public static IEnumerable<Type> GetTypesAssignableFrom<TType>() where TType : class
    {
        return GetAllLoadableTypes().Where(t => !t.IsAbstract && typeof(TType).IsAssignableFrom(t));
    }
}
