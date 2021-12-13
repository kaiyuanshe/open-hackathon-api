using System.Collections.Generic;
using System.Diagnostics;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public abstract class StorageClientBase
    {
        public IStorageCredentialProvider StorageCredentialProvider { get; set; }

        public ITraceIdHttpPipelinePolicyFactory TraceIdHttpPipelinePolicyFactory { get; set; }

        public abstract string StorageName { get; }

        protected IDictionary<string, object> GetMessageProperties()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            string traceId = Activity.Current?.Id ?? string.Empty;
            properties.Add(HttpHeaderNames.TraceId, traceId);
            return properties;
        }
    }
}
