﻿
/* Unmerged change from project 'ServiceStack.Redis.Core (netstandard2.0)'
Before:
using System;
After:
using ServiceStack.Redis.Support;
using System;
*/
using ServiceStack.Redis.Support;
using System;

/* Unmerged change from project 'ServiceStack.Redis.Core (netstandard2.0)'
Before:
using System.Text;
using ServiceStack.Redis.Support;
After:
using System.Text;
*/
using System.Collections.Generic;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Provides sharding of redis client connections.
    /// uses consistent hashing to distribute keys across connection pools
    /// </summary>
    public class ShardedRedisClientManager
    {
        private readonly ConsistentHash<ShardedConnectionPool> _consistentHash;

        public ShardedRedisClientManager(params ShardedConnectionPool[] connectionPools)
        {
            if (connectionPools == null) throw new ArgumentNullException("connection pools can not be null.");

            List<KeyValuePair<ShardedConnectionPool, int>> pools = new List<KeyValuePair<ShardedConnectionPool, int>>();
            foreach (var connectionPool in connectionPools)
            {
                pools.Add(new KeyValuePair<ShardedConnectionPool, int>(connectionPool, connectionPool.weight));
            }
            _consistentHash = new ConsistentHash<ShardedConnectionPool>(pools);
        }

        /// <summary>
        /// maps a key to a redis connection pool
        /// </summary>
        /// <param name="key">key to map</param>
        /// <returns>a redis connection pool</returns>
        public ShardedConnectionPool GetConnectionPool(string key)
        {
            return _consistentHash.GetTarget(key);
        }
    }
}
