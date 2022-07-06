using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server
{
    public static class TypeHelper
    {
        /// <summary>
        /// Get all instantiable sub types in the same assembly
        /// </summary>
        /// <param name="abstractType"></param>
        /// <returns></returns>
        public static Type[] SubTypes(this Type abstractType)
        {
            Type[] theTypes = abstractType.Assembly.GetTypes();
            return theTypes.Where(t => (!t.IsAbstract) && abstractType.IsAssignableFrom(t)).ToArray();
        }

        /// <summary>
        /// Get all instantiable sub types in the declaring assembly and specified assembly
        /// </summary>
        /// <param name="abstractType"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Type[] SubTypes(this Type abstractType, params Assembly[] assemblies)
        {
            IEnumerable<Assembly> searchingAssemblies = new List<Assembly> { abstractType.Assembly };
            if (assemblies != null)
            {
                searchingAssemblies = searchingAssemblies.Concat(assemblies.Where(a => a != abstractType.Assembly));
            }

            IEnumerable<Type> theTypes = searchingAssemblies.SelectMany(a => a.GetTypes());
            return theTypes.Where(t => (!t.IsAbstract) && abstractType.IsAssignableFrom(t)).ToArray();
        }

        /// <summary>
        /// Get all instantiable sub types of a generic type in the declaring assembly and specified assembly
        /// </summary>
        public static Type[] GenericSubTypes(this Type abstractType, params Assembly[] assemblies)
        {
            var parentType = ResolveGenericTypeDefinition(abstractType);

            IEnumerable<Assembly> searchingAssemblies = new List<Assembly> { abstractType.Assembly };
            if (assemblies != null)
            {
                searchingAssemblies = searchingAssemblies.Concat(assemblies.Where(a => a != abstractType.Assembly));
            }
            IEnumerable<Type> theTypes = searchingAssemblies.SelectMany(a => a.GetTypes());

            return theTypes.Where(t => (!t.IsAbstract) && t.InheritsOrImplements(parentType)).ToArray();
        }

        public static IEnumerable<Type> GetInterfaces(this Type type, bool includeInherited)
        {
            if (includeInherited || type.BaseType == null)
                return type.GetInterfaces();
            else
                return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
        }

        public static bool InheritsOrImplements(this Type child, Type parent)
        {
            parent = ResolveGenericTypeDefinition(parent);

            var currentChild = child.IsGenericType ? child.GetGenericTypeDefinition() : child;
            while (currentChild != typeof(object))
            {
                if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
                    return true;

                currentChild = currentChild.BaseType != null && currentChild.BaseType.IsGenericType
                                   ? currentChild.BaseType.GetGenericTypeDefinition()
                                   : currentChild.BaseType;

                if (currentChild == null)
                    return false;
            }
            return false;
        }

        public static bool IsDateTime(this Type type)
        {
            if (type == typeof(DateTime))
                return true;

            if (Nullable.GetUnderlyingType(type) == typeof(DateTime))
                return true;

            return false;
        }

        public static TDestination As<TDestination>(this object src, Action<TDestination>? configure = null)
            where TDestination : new()
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            TDestination resp = new TDestination();
            var srcProperties = src.GetType().GetProperties();
            foreach (var property in typeof(TDestination).GetProperties())
            {
                var srcProp = srcProperties.FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
                if (srcProp != null)
                {
                    var srcValue = srcProp.GetValue(src);
                    if (srcValue == null)
                        continue;

                    if (IsTypeMatched(srcProp.PropertyType, property.PropertyType))
                    {
                        property.SetValue(resp, srcValue);
                    }
                    else
                    {
                        // type mismatch, try changing type
                        property.SetValue(resp, ConvertType(srcProp.PropertyType, property.PropertyType, srcValue));
                    }
                }
            }
            if (configure != null)
            {
                configure(resp);
            }

            return resp;
        }

        private static bool IsTypeMatched(Type srcType, Type objectType)
        {
            return GetRealType(srcType) == GetRealType(objectType);
        }

        private static Type GetRealType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        private static object ConvertType(Type srcType, Type objectType, object value)
        {
            if (srcType == objectType)
                return value;

            // handle datetime for better format.
            if (srcType == typeof(DateTime) && objectType == typeof(string))
            {
                // 2021-02-11T01:51:28.1855155Z
                return ((DateTime)value).ToString("o");
            }

            return Convert.ChangeType(value, objectType);
        }

        private static Type ResolveGenericTypeDefinition(Type parent)
        {
            if (parent.IsGenericType && parent.GetGenericTypeDefinition() == parent)
            {
                return parent.GetGenericTypeDefinition();
            }

            return parent;
        }

        private static bool HasAnyInterfaces(Type parent, Type child)
        {
            return child.GetInterfaces()
                .Any(childInterface =>
                {
                    var currentInterface = childInterface.IsGenericType ? childInterface.GetGenericTypeDefinition() : childInterface;
                    return currentInterface == parent;
                });
        }
    }
}
