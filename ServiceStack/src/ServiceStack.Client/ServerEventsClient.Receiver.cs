﻿using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceStack
{
    public delegate void ServerEventCallback(ServerEventsClient source, ServerEventMessage args);

    public class ServerEventReceiver : IReceiver
    {
        public static ILog Log = LogManager.GetLogger(typeof(ServerEventReceiver));

        public ServerEventsClient Client { get; set; }
        public ServerEventMessage Request { get; set; }

        public virtual void NoSuchMethod(string selector, object message)
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"NoSuchMethod defined for {selector}");
        }
    }

    public class NewInstanceResolver : IResolver
    {
        public T TryResolve<T>()
        {
            return typeof(T).CreateInstance<T>();
        }
    }

    public class SingletonInstanceResolver : IResolver
    {
        readonly ConcurrentDictionary<Type, object> Cache = new ConcurrentDictionary<Type, object>();

        public T TryResolve<T>()
        {
            return (T)Cache.GetOrAdd(typeof(T), type => type.CreateInstance<T>());
        }
    }

    public partial class ServerEventsClient
    {
        public IResolver Resolver { get; set; }

        public ConcurrentDictionary<string, ServerEventCallback> Handlers { get; }
        public ConcurrentDictionary<string, ServerEventCallback> NamedReceivers { get; }
        public List<Type> ReceiverTypes { get; }
        public bool StrictMode { get; set; }

        public ServerEventsClient RegisterReceiver<T>()
            where T : IReceiver
        {
            return RegisterNamedReceiver<T>("cmd");
        }

        public ServerEventsClient RegisterNamedReceiver<T>(string receiverName)
            where T : IReceiver
        {
            ReceiverExec<T>.Reset();

            lock (NamedReceivers)
            {
                NamedReceivers[receiverName] = CreateReceiverHandler<T>;

                ReceiverTypes.Add(typeof(T));
            }

            return this;
        }

        private void CreateReceiverHandler<T>(ServerEventsClient client, ServerEventMessage msg)
        {
            if (!(Resolver.TryResolve<T>() is IReceiver receiver))
                throw new ArgumentNullException("receiver", "Resolver returned null receiver");

            if (receiver is ServerEventReceiver injectRecevier)
            {
                injectRecevier.Client = client;
                injectRecevier.Request = msg;
            }

            var target = msg.Target.Replace("-", ""); //css bg-image

            ReceiverExec<T>.RequestTypeExecMap.TryGetValue(target, out var receiverCtx);
            if (StrictMode && receiverCtx != null && !receiverCtx.Method.EqualsIgnoreCase(target))
                receiverCtx = null;

            if (receiverCtx == null)
                ReceiverExec<T>.MethodNameExecMap.TryGetValue(target, out receiverCtx);

            if (receiverCtx == null)
            {
                receiver.NoSuchMethod(msg.Target, msg);
                return;
            }

            object requestDto;
            try
            {
                requestDto = string.IsNullOrEmpty(msg.Json)
                    ? receiverCtx.RequestType.CreateInstance()
                    : JsonSerializer.DeserializeFromString(msg.Json, receiverCtx.RequestType);
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Could not deserialize into '{typeof(T).Name}' from '{msg.Json}'", ex);
            }

            receiverCtx.Exec(receiver, requestDto);
        }
    }

    internal delegate object ActionInvokerFn(object intance, object request);
    internal delegate object ActionNoArgInvokerFn(object intance);
    internal delegate void VoidActionInvokerFn(object intance, object request);
    internal delegate void VoidActionNoArgInvokerFn(object intance);

    internal class ReceiverExecContext
    {
        public const string AnyAction = "ANY";
        public string Id { get; set; }
        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public string Method { get; set; }
        public ActionInvokerFn Exec { get; set; }

        public static string Key(string method)
        {
            return method.ToUpper();
        }

        public static string Key(string method, string requestDtoName)
        {
            return method.ToUpper() + " " + requestDtoName;
        }

        public static string AnyKey(string requestDtoName)
        {
            return AnyAction + " " + requestDtoName;
        }
    }

    internal class ReceiverExec<T>
    {
        internal static Dictionary<string, ReceiverExecContext> RequestTypeExecMap;
        internal static Dictionary<string, ReceiverExecContext> MethodNameExecMap;

        public static void Reset()
        {
            RequestTypeExecMap = new Dictionary<string, ReceiverExecContext>(StringComparer.OrdinalIgnoreCase);
            MethodNameExecMap = new Dictionary<string, ReceiverExecContext>(StringComparer.OrdinalIgnoreCase);

            var methods = typeof(T).GetMethods().Where(x => x.IsPublic && !x.IsStatic);
            foreach (var mi in methods)
            {
                var actionName = mi.Name;
                var args = mi.GetParameters();
                if (args.Length > 1)
                    continue;
                if (mi.Name.StartsWith("get_"))
                    continue;
                if (mi.DeclaringType == typeof(object))
                    continue;
                if (mi.Name == "Equals")
                    continue;

                if (actionName.StartsWith("set_"))
                    actionName = actionName.Substring("set_".Length);

                if (args.Length == 0)
                {
                    var voidExecCtx = new ReceiverExecContext
                    {
                        Id = ReceiverExecContext.Key(actionName),
                        ServiceType = typeof(T),
                        RequestType = null,
                        Method = mi.Name
                    };
                    MethodNameExecMap[actionName] = voidExecCtx;
                    voidExecCtx.Exec = CreateExecFn(mi);
                    continue;
                }

                var requestType = args[0].ParameterType;
                var execCtx = new ReceiverExecContext
                {
                    Id = ReceiverExecContext.Key(actionName, requestType.GetOperationName()),
                    ServiceType = typeof(T),
                    RequestType = requestType,
                    Method = mi.Name
                };

                try
                {
                    execCtx.Exec = CreateExecFn(requestType, mi);
                }
                catch
                {
                    //Potential problems with MONO, using reflection for fallback
                    execCtx.Exec = (receiver, request) =>
                        mi.Invoke(receiver, new[] { request });
                }

                RequestTypeExecMap[requestType.Name] = execCtx;
                MethodNameExecMap[actionName] = execCtx;
            }
        }

        private static ActionInvokerFn CreateExecFn(Type requestType, MethodInfo mi)
        {
            var receiverType = typeof(T);

            var receiverParam = Expression.Parameter(typeof(object), "receiverObj");
            var receiverStrong = Expression.Convert(receiverParam, receiverType);

            var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
            var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

            Expression callExecute = Expression.Call(
            receiverStrong, mi, requestDtoStrong);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                (callExecute, receiverParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                (callExecute, receiverParam, requestDtoParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service, request);
                    return null;
                };
            }
        }

        private static ActionInvokerFn CreateExecFn(MethodInfo mi)
        {
            var receiverType = typeof(T);

            var receiverParam = Expression.Parameter(typeof(object), "receiverObj");
            var receiverStrong = Expression.Convert(receiverParam, receiverType);

            Expression callExecute = Expression.Call(receiverStrong, mi);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionNoArgInvokerFn>
                    (callExecute, receiverParam).Compile();

                return (service, request) => executeFunc(service);
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionNoArgInvokerFn>
                    (callExecute, receiverParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service);
                    return null;
                };
            }
        }
    }
}