using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    public abstract class BaseTableEntity
    {
        /// <summary>
        /// The partition key is a unique identifier for the partition within a given table and forms the first part of an entity's primary key.
        /// </summary>
        /// <value>A string containing the partition key for the entity.</value>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The row key is a unique identifier for an entity within a given partition. Together the <see cref="P:Azure.Data.Tables.ITableEntity.PartitionKey" /> and RowKey uniquely identify every entity within a table.
        /// </summary>
        /// <value>A string containing the row key for the entity.</value>
        public string RowKey { get; set; }

        /// <summary>
        /// The Timestamp property is a DateTime value that is maintained on the server side to record the time an entity was last modified.
        /// The Table service uses the Timestamp property internally to provide optimistic concurrency. The value of Timestamp is a monotonically increasing value,
        /// meaning that each time the entity is modified, the value of Timestamp increases for that entity.
        /// This property should not be set on insert or update operations (the value will be ignored).
        /// </summary>
        /// <value>A <see cref="T:System.DateTimeOffset" /> containing the timestamp of the entity.</value>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the entity's ETag.
        /// </summary>
        /// <value>A string containing the ETag value for the entity.</value>
        public string ETag { get; set; }

        /// <summary>
        /// datetime when the entity is created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    public sealed class DynamicTableEntity : BaseTableEntity
    {
        public DynamicTableEntity()
        {
            Properties = new Dictionary<string, object>();
        }

        public DynamicTableEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            Properties = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Properties { get; set; }
    }

    public static class BaseTableEntityExtensions
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertyDictionary = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        public static TableEntity? ToTableEntity(this BaseTableEntity entity)
        {
            if (entity == null)
                return null;

            if (entity is DynamicTableEntity dynamicEntity)
            {
                return dynamicEntity.ToTableEntityDynamic();
            }

            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (var property in GetCachedProperties(entity.GetType()))
            {
                // Skip ignored property
                if (Attribute.IsDefined(property, typeof(IgnoreEntityPropertyAttribute)))
                {
                    continue;
                }

                Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                var propertyValue = property.GetValue(entity);
                if (propertyValue == null)
                {
                    continue;
                }

                var convertableAttribute = property.GetCustomAttribute<ConvertableEntityPropertyAttribute>();
                if (convertableAttribute != null)
                {
                    values.Add(property.Name, convertableAttribute.Serialize(propertyValue));
                }
                else if (property.PropertyType.IsEnum || (nullableType != null && nullableType.IsEnum))
                {
                    values.Add(property.Name, (int)propertyValue);
                }
                else if (property.Name == nameof(ITableEntity.ETag))
                {
                    continue;
                }
                else if (property.PropertyType == typeof(DateTime) || nullableType == typeof(DateTime))
                {
                    if (((DateTime)propertyValue).Year < 1700)
                    {
                        continue;
                    }
                    if (propertyValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                    {
                        values.Add(property.Name, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                    }
                    else
                    {
                        values.Add(property.Name, propertyValue);
                    }
                }
                else
                {
                    values.Add(property.Name, propertyValue);
                }
            }

            var tableEntity = new TableEntity(values);
            tableEntity.ETag = new ETag(entity.ETag);
            return tableEntity;
        }

        public static TableEntity ToTableEntityDynamic(this DynamicTableEntity entity)
        {
            var tableEntity = new TableEntity(entity.Properties);
            tableEntity.PartitionKey = entity.PartitionKey;
            tableEntity.RowKey = entity.RowKey;
            tableEntity.Timestamp = entity.Timestamp;
            tableEntity.ETag = new ETag(entity.ETag);

            return tableEntity;
        }

        public static TEntity? ToBaseTableEntity<TEntity>(this TableEntity tableEntity)
            where TEntity : BaseTableEntity, new()
        {
            if (tableEntity == null)
            {
                return default(TEntity);
            }

            if (typeof(TEntity) == typeof(DynamicTableEntity))
            {
                return (TEntity)(object)tableEntity.ToDynamicTableEntity();
            }

            TEntity entity = new TEntity();
            foreach (var property in GetCachedProperties(entity.GetType()))
            {
                // Skip ignored property
                if (Attribute.IsDefined(property, typeof(IgnoreEntityPropertyAttribute)))
                {
                    continue;
                }

                Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                // Convertable property
                var convertableAttribute = property.GetCustomAttribute<ConvertableEntityPropertyAttribute>();
                if (convertableAttribute != null && tableEntity.ContainsKey(property.Name))
                {
                    Type resultType = property.PropertyType;
                    if (convertableAttribute.ConvertToType != null)
                    {
                        resultType = convertableAttribute.ConvertToType;
                    }

                    var objectValue = convertableAttribute.Deserialize(tableEntity.GetString(property.Name), resultType);
                    // Set property only when deserialized value is not null,
                    // otherwise leave it to be the default value as constructed
                    if (objectValue != null)
                    {
                        property.SetValue(entity, objectValue);
                    }
                }
                else if (!tableEntity.ContainsKey(property.Name) || tableEntity[property.Name] == null)
                {
                    var backwardCompatibleAttributes = Attribute.GetCustomAttributes(property, typeof(BackwardCompatibleAttribute));
                    foreach (BackwardCompatibleAttribute attribute in backwardCompatibleAttributes)
                    {
                        var value = attribute.GetValue(tableEntity);
                        if (value != null)
                        {
                            property.SetValue(entity, value);
                            break;
                        }
                    }
                }
                else if (property.PropertyType.IsEnum
                    || (nullableType != null && nullableType.IsEnum))
                {
                    // Enum property
                    var enumType = property.PropertyType.IsEnum ? property.PropertyType : nullableType;
                    var value = tableEntity.GetInt32(property.Name);
                    if (value.HasValue)
                    {
                        Debug.Assert(enumType != null);
                        property.SetValue(entity, Enum.ToObject(enumType, value.Value));
                    }
                }
                else if (property.Name == nameof(ITableEntity.ETag))
                {
                    continue;
                }
                else if (property.PropertyType == typeof(byte[]))
                {
                    // TryGetValue for byte[] returns BinaryData.
                    var bytes = tableEntity.GetBinary(property.Name);
                    property.SetValue(entity, bytes);
                }
                else if (property.PropertyType == typeof(DateTime) || nullableType == typeof(DateTime))
                {
                    // TryGetValue always returns DateTimeOffset.
                    var date = tableEntity.GetDateTimeOffset(property.Name);
                    if (date.HasValue)
                    {
                        property.SetValue(entity, date.GetValueOrDefault().UtcDateTime);
                    }
                }
                else if (tableEntity.TryGetValue(property.Name, out object value) && value != null)
                {
                    property.SetValue(entity, value);
                }
            }

            entity.ETag = tableEntity.ETag.ToString();
            return entity;
        }

        public static DynamicTableEntity ToDynamicTableEntity(this TableEntity tableEntity)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>(tableEntity);
            var entity = new DynamicTableEntity
            {
                Properties = properties,
                PartitionKey = tableEntity.PartitionKey,
                RowKey = tableEntity.RowKey,
                ETag = tableEntity.ETag.ToString(),
                Timestamp = tableEntity.Timestamp.GetValueOrDefault(),
            };
            return entity;
        }

        public static IEnumerable<TEntity?> ToBaseTableEntities<TEntity>(this IEnumerable<TableEntity> tableEntities)
            where TEntity : BaseTableEntity, new()
        {
            return tableEntities.Select(entity => entity.ToBaseTableEntity<TEntity>());
        }

        private static IEnumerable<PropertyInfo> GetCachedProperties(Type type)
        {
            if (!_propertyDictionary.TryGetValue(type, out IEnumerable<PropertyInfo>? properties))
            {
                properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                _propertyDictionary.TryAdd(type, properties);
            }

            return properties;
        }
    }
}
