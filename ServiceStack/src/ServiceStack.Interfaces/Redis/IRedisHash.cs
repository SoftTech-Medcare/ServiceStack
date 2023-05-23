using ServiceStack.Model;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
    public interface IRedisHash
        : IDictionary<string, string>, IHasStringId
    {
        bool AddIfNotExists(KeyValuePair<string, string> item);
        void AddRange(IEnumerable<KeyValuePair<string, string>> items);
        long IncrementValue(string key, int incrementBy);
    }
}