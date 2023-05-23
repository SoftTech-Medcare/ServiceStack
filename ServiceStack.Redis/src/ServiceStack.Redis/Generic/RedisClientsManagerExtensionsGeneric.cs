namespace ServiceStack.Redis.Generic
{
    public static class RedisClientsManagerExtensionsGeneric
    {
        public static ManagedList<T> GetManagedList<T>(this IRedisClientsManager manager, string key)
        {
            return new ManagedList<T>(manager, key);
        }
    }
}
