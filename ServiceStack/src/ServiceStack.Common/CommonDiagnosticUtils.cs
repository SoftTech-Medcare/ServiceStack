
/* Unmerged change from project 'ServiceStack.Common.Core (netstandard2.0)'
Before:
using System;
After:
using ServiceStack.Messaging;
using System;
*/
using ServiceStack.Messaging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ServiceStack;

// sync with ClientDiagnosticUtils
public static class CommonDiagnosticUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Init(this DiagnosticListener listener, IMessage msg)
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteMqRequestBefore))
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                msg.TraceId ??= activity.GetTraceId();
                msg.Tag ??= activity.GetTag();
            }
        }
    }
}
