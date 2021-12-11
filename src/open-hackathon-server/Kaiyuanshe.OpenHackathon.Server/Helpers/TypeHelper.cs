using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
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

        public static TDestination As<TDestination>(this object src, Action<TDestination> configure = null)
            where TDestination : new()
        {
            if (src == null)
            {
                return default;
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

        public static DynamicTableEntity ToTableEntity(this object model, string partitionKey, string rowKey, Action<DynamicTableEntity> configure = null)
        {
            DynamicTableEntity tableEntity = new DynamicTableEntity(partitionKey, rowKey);

            foreach (var property in model.GetType().GetProperties())
            {
                var srcValue = property.GetValue(model);
                if (srcValue == null)
                    continue;

                if (property.PropertyType == typeof(string))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty(srcValue.ToString()));
                }
                else if (property.PropertyType == typeof(int)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(int))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((int)srcValue));
                }
                else if (property.PropertyType == typeof(bool)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((bool)srcValue));
                }
                else if (property.PropertyType == typeof(DateTime)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(DateTime))
                {
                    DateTime srcDate = (DateTime)srcValue;
                    if (srcDate > new DateTime(1900, 1, 1))
                    {
                        tableEntity.Properties.Add(property.Name, new EntityProperty((DateTime)srcValue));
                    }
                }
                else if (property.PropertyType == typeof(long)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(long))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((long)srcValue));
                }
                else if (property.PropertyType == typeof(double)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(double))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((double)srcValue));
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((Guid)srcValue));
                }
                else if (property.PropertyType.IsEnum
                    || (Nullable.GetUnderlyingType(property.PropertyType) != null && Nullable.GetUnderlyingType(property.PropertyType).IsEnum))
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty((int)srcValue));
                }
                else
                {
                    tableEntity.Properties.Add(property.Name, new EntityProperty(JsonConvert.SerializeObject(srcValue)));
                }
            }

            if (configure != null)
            {
                configure(tableEntity);
            }

            return tableEntity;
        }

        public static TModel ToModel<TModel>(this DynamicTableEntity tableEntity, TModel model, Action<TModel> configure = null)
        {
            foreach (var property in model.GetType().GetProperties())
            {
                if (tableEntity == null || !tableEntity.Properties.ContainsKey(property.Name))
                    continue;

                if (property.PropertyType == typeof(string))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].StringValue);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(int)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(int))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].Int32Value);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(bool)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].BooleanValue);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(DateTime)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(DateTime))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].DateTime);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(long)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(long))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].Int64Value);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(double)
                    || Nullable.GetUnderlyingType(property.PropertyType) == typeof(double))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].DoubleValue);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].GuidValue);
                    }
                    catch
                    {
                    }
                }
                else if (property.PropertyType.IsEnum
                    || (Nullable.GetUnderlyingType(property.PropertyType) != null && Nullable.GetUnderlyingType(property.PropertyType).IsEnum))
                {
                    try
                    {
                        property.SetValue(model, tableEntity.Properties[property.Name].Int32Value);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    try
                    {
                        var str = tableEntity.Properties[property.Name].StringValue;
                        property.SetValue(model, JsonConvert.DeserializeObject(str, property.PropertyType));
                    }
                    catch
                    {
                    }
                }
            }

            if (configure != null)
            {
                configure(model);
            }

            return model;
        }

        private static bool IsTypeMatched(Type srcType, Type objectType)
        {
            return GetRealType(srcType) == GetRealType(objectType);
        }

        private static Type GetRealType(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                return Nullable.GetUnderlyingType(type);

            return type;
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

            try
            {
                return Convert.ChangeType(value, objectType);
            }
            catch
            {
            }

            return null;
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
