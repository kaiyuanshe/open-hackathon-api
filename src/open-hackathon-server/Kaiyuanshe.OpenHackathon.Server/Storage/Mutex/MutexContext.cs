using System.Threading;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Mutex
{
    public interface IMutexContext
    {
        string LeaseId { get; }
        CancellationToken LockReleased { get; }
    }

    internal class MutexContext : IMutexContext
    {
        public string LeaseId { get; internal set; }

        public CancellationToken LockReleased { get; internal set; }

    }
}
