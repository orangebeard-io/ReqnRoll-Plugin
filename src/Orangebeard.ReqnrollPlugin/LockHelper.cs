using System.Collections.Concurrent;

namespace Orangebeard.ReqnrollPlugin
{
    internal static class LockHelper
    {
        private static readonly ConcurrentDictionary<int, object> Repository = new ConcurrentDictionary<int, object>();

        private static readonly object GetLockLock = new object();

        public static object GetLock(int hashCode)
        {
            lock (GetLockLock)
            {
                if (Repository.TryGetValue(hashCode, out var @lock))
                {
                    return @lock;
                }

                var lockObj = new object();
                Repository.AddOrUpdate(hashCode, lockObj, (key, oldValue) => oldValue);

                return lockObj;
            }
        }
    }
}
