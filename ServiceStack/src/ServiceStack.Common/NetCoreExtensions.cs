#if NETCORE

/* Unmerged change from project 'ServiceStack.Common.Core (netstandard2.0)'
Before:
using System.Data;
After:
using System.Data.Common;
*/
using System.Net.Sockets;

namespace ServiceStack
{
    public static class NetCoreExtensions
    {
        public static void Close(this Socket socket)
        {
            socket.Dispose();
        }

        public static void Close(this DbDataReader reader)
        {
            reader.Dispose();
        }
    }
}
#endif