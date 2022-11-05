﻿namespace Kaiyuanshe.OpenHackathon.Server
{
    public class ConfigurationKeys
    {
        public const string RedisCacheEnabled = "Redis:Enabled";
        public const string RedisCachePassword = "Redis:Password";
        // Add to env variable "Guacamole__TrustedApps" to set the value.
        public const string GuacamoleTrustedApps = "Guacamole:TrustedApps";
        public const string StorageConnectionString = "Storage:Hackathon:ConnectionString";
    }
}
