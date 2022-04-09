﻿using System;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server.Helpers
{
    public static class EnumHelper
    {
        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The first attribute of type T that exists on the enum value</returns>
        public static T GetCustomAttribute<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(Enum.GetName(type, enumVal));
            return memInfo[0].GetCustomAttribute<T>(false);
        }
    }
}
