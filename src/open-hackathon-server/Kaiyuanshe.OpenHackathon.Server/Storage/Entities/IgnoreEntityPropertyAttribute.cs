using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    //
    // Summary:
    //     Represents a custom attribute that can be used to ignore entity properties during
    //     serialization/de-serialization.
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreEntityPropertyAttribute : Attribute
    {
    }
}
