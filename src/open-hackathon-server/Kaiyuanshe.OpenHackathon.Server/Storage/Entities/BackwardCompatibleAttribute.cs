using Azure.Data.Tables;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// An attribute to help us rename a property while keeps backward compatibility to existing data
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class BackwardCompatibleAttribute : Attribute
    {
        public string _compatibleName;

        /// <summary>
        /// Populate value from a existing property
        /// </summary>
        /// <param name="compatibleName">Existing property name</param>
        public BackwardCompatibleAttribute(string compatibleName)
        {
            if (string.IsNullOrEmpty(compatibleName))
            {
                throw new ArgumentNullException("Compatible name is null or empty");
            }
            _compatibleName = compatibleName;
        }
    }

    internal static class BackwardCompatibleAttributeExtensions
    {
        public static object GetValue(this BackwardCompatibleAttribute attribute, TableEntity tableEntity)
        {
            if (attribute._compatibleName != null)
            {
                if (tableEntity.TryGetValue(attribute._compatibleName, out object value))
                {
                    if (value != null)
                        return value;
                }

                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
