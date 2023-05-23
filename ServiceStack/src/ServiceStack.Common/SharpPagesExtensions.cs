using ServiceStack.Text;
using ServiceStack.Web;

/* Unmerged change from project 'ServiceStack.Common.Core (netstandard2.0)'
Before:
using ServiceStack.Text;
using ServiceStack.Web;
After:
using ServiceStack.IO;
using System.Threading.Web;
*/
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class SharpPagesExtensions
    {
        public static async Task<string> RenderToStringAsync(this IStreamWriterAsync writer)
        {
            using var ms = MemoryStreamFactory.GetStream();
            await writer.WriteToAsync(ms);
            return await ms.ReadToEndAsync();
        }
    }
}