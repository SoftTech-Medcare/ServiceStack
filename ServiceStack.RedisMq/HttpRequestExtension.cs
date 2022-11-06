using ServiceStack.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceStack
{
    public static class HttpRequestExtension
    {
        public static int ToStatusCode(this Exception ex)
        {
            if (ex is AggregateException aex)
            {
                if (aex.InnerExceptions.Count == 1)
                    ex = aex.InnerExceptions[0];
                else
                    return ToStatusCode(aex.InnerExceptions[0]);
            }

            if (ex is IHasStatusCode hasStatusCode)
                return hasStatusCode.StatusCode;

            if (ex is WebException webEx)
                return (int)webEx.GetStatus().GetValueOrDefault(HttpStatusCode.InternalServerError);

            if (ex is HttpError httpEx) return httpEx.Status;
            if (ex is NotImplementedException || ex is NotSupportedException) return (int)HttpStatusCode.MethodNotAllowed;
            if (ex is FileNotFoundException) return (int)HttpStatusCode.NotFound;
            if (ex is ArgumentException || ex is SerializationException || ex is FormatException) return (int)HttpStatusCode.BadRequest;
            if (ex is AuthenticationException) return (int)HttpStatusCode.Unauthorized;
            if (ex is UnauthorizedAccessException) return (int)HttpStatusCode.Forbidden;
            if (ex is OptimisticConcurrencyException) return (int)HttpStatusCode.Conflict;
            return (int)HttpStatusCode.InternalServerError;
        }
    }
}
