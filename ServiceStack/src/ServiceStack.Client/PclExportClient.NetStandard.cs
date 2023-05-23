//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETCORE

/* Unmerged change from project 'ServiceStack.Client.Core (netstandard2.0)'
Before:
using System.Collections.Specialized;
using System.IO;
After:
using System.Pcl;
using System.Web;
*/
using System;
using System.Collections.Generic;
/* Unmerged change from project 'ServiceStack.Client.Core (netstandard2.0)'
Before:
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Pcl;
using System.Collections.Generic;
using System.Globalization;
After:
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
*/



namespace ServiceStack
{
    public class NetStandardPclExportClient : PclExportClient
    {
        public static NetStandardPclExportClient Provider = new NetStandardPclExportClient();

        static readonly Dictionary<string, bool> multiHeaders = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {
                {HttpHeaders.Allow,              false},
                {HttpHeaders.Accept,             false},
                {HttpHeaders.Authorization,      false},
                {HttpHeaders.AcceptRanges,       false},
                {HttpHeaders.AcceptCharset,      false},
                {HttpHeaders.AcceptEncoding,     false},
                {HttpHeaders.AcceptLanguage,     false},
                {HttpHeaders.Cookie,             false},
                {HttpHeaders.Connection,         false},
                {HttpHeaders.CacheControl,       false},
                {HttpHeaders.ContentEncoding,    false},
                {HttpHeaders.ContentLanguage,    false},
                {HttpHeaders.Expect,             false},
                {HttpHeaders.IfMatch,            false},
                {HttpHeaders.IfNoneMatch,        false},
                {HttpHeaders.Pragma,             false},
                {HttpHeaders.ProxyAuthenticate,  false},
                {HttpHeaders.ProxyAuthorization, false},
                {HttpHeaders.ProxyConnection,    false},
                {HttpHeaders.Range,              false},
                {HttpHeaders.SetCookie,          false},
                {HttpHeaders.SetCookie2,         false},
                {HttpHeaders.TE,                 false},
                {HttpHeaders.Trailer,            false},
                {HttpHeaders.TransferEncoding,   false},
                {HttpHeaders.Upgrade,            false},
                {HttpHeaders.Via,                false},
                {HttpHeaders.Vary,               false},
                {HttpHeaders.Warning,            false}
            };

        static readonly Action<HttpWebRequest, DateTime> SetIfModifiedSinceDelegate =
                    (Action<HttpWebRequest, DateTime>)typeof(HttpWebRequest)
                        .GetProperty("IfModifiedSince")
                        ?.GetSetMethod(nonPublic: true)
                        ?.CreateDelegate(typeof(Action<HttpWebRequest, DateTime>));

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new NetStandardPclExportClient()));
            NetStandardPclExport.Configure();
            return Provider;
        }

        public override void SetIfModifiedSince(HttpWebRequest webReq, DateTime lastModified)
        {
            //support for Xamarin and .NET platform
            if (SetIfModifiedSinceDelegate != null)
            {
                SetIfModifiedSinceDelegate(webReq, lastModified);
            }
            else
            {
#if NETCORE
                if (lastModified == DateTime.MinValue)
                    webReq.Headers.Remove(HttpHeaders.IfModifiedSince);
                else
                    webReq.Headers[HttpHeaders.IfModifiedSince] = lastModified.ToUniversalTime().ToString("R", new DateTimeFormatInfo());
#else
                    webReq.Headers[HttpHeaders.IfModifiedSince] = lastModified.ToUniversalTime().ToString("R", new DateTimeFormatInfo());
#endif
            }
        }

        public override string GetHeader(WebHeaderCollection headers, string name, Func<string, bool> valuePredicate)
        {
            var values = GetValues(headers, name);
            return values?.FirstOrDefault(valuePredicate);

            /* Unmerged change from project 'ServiceStack.Client.Core (netstandard2.0)'
            Before:
                    }

                    //see .NET 4.6.2 Reference source
            After:
                    }

                    //see .NET 4.6.2 Reference source
            */
        }

        //see .NET 4.6.2 Reference source
        private static string[] GetValues(WebHeaderCollection headers, string header)
        {

            /* Unmerged change from project 'ServiceStack.Client.Core (netstandard2.0)'
            Before:
                        var value = headers[header];

                        if (value == null)
            After:
                        var value = headers[header];

                        if (value == null)
            */
            var value = headers[header];

            if (value == null)
                return null;

            if (!multiHeaders.ContainsKey(header))
                return new string[1] { value };

            var tempStringCollection = new List<string>();

            bool inquote = false;
            int chIndex = 0;
            char[] vp = new char[value.Length];
            string singleValue;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\"')
                {
                    inquote = !inquote;
                }
                else if ((value[i] == ',') && !inquote)
                {
                    singleValue = new string(vp, 0, chIndex);
                    tempStringCollection.Add(singleValue.Trim());
                    chIndex = 0;
                    continue;
                }
                vp[chIndex++] = value[i];
            }

            if (chIndex != 0)
            {
                singleValue = new string(vp, 0, chIndex);
                tempStringCollection.Add(singleValue.Trim());
            }

            return tempStringCollection.ToArray();
        }
    }

    public class AsyncTimer : ITimer
    {
        public System.Threading.Timer Timer;

        public AsyncTimer(System.Threading.Timer timer)
        {
            Timer = timer;
        }

        public void Cancel()
        {
            if (Timer == null) return;

            this.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.Dispose();
        }

        public void Dispose()
        {
            if (Timer == null) return;

            this.Timer.Dispose();
            this.Timer = null;
        }
    }
}
#endif
