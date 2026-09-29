using System.Reflection;

namespace StarterKit.Shared.Utilities;

public static class ReflectionHelper
{
    public static IList<string> GetPublicConstants(Type type)
    {
        var constants = new List<string>();

        // get classes in class
        var modules = type.GetNestedTypes();

        foreach (var module in modules)
        {
            // get props in class
            var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo fi in fields)
            {
                var propertyValue = fi.GetValue(null)?.ToString();

                if (propertyValue != null)
                {
                    constants.Add(propertyValue);
                }
            }
        }

        return constants;
    }
}
