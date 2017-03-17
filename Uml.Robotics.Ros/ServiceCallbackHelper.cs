﻿using System.Collections.Generic;
using Messages;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public delegate bool ServiceFunction<in MReq, MRes>(MReq req, ref MRes res)
        where MReq : RosMessage, new()
        where MRes : RosMessage, new();

    public class ServiceCallbackHelperParams<MReq, MRes> : IServiceCallbackHelperParams
    {
        public new MReq request;
        public new MRes response;
    }

    public class IServiceCallbackHelperParams
    {
        public IDictionary<string, string> connection_header;
        public RosMessage request, response;
    }

    public class ServiceCallbackHelper<MReq, MRes> : IServiceCallbackHelper
        where MReq : RosMessage, new()
        where MRes : RosMessage, new()
    {
        protected new ServiceFunction<MReq, MRes> _callback;

        public ServiceCallbackHelper(ServiceFunction<MReq, MRes> srv_func)
        {
            // TODO: Complete member initialization
            _callback = srv_func;
        }

        internal bool call(ServiceCallbackHelperParams<MReq, MRes> parms)
        {
            return _callback.Invoke(parms.request, ref parms.response);
        }
    }

    public class IServiceCallbackHelper
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<IServiceCallbackHelper>();
        protected ServiceFunction<RosMessage, RosMessage> _callback;

        public MsgTypes type;

        protected IServiceCallbackHelper()
        {
            // Logger.LogDebug("ISubscriptionCallbackHelper: 0 arg constructor");
        }

        protected IServiceCallbackHelper(ServiceFunction<RosMessage, RosMessage> Callback)
        {
            //Logger.LogDebug("ISubscriptionCallbackHelper: 1 arg constructor");
            //throw new NotImplementedException();
            _callback = Callback;
        }

        public virtual ServiceFunction<RosMessage, RosMessage> callback()
        {
            return _callback;
        }

        public virtual ServiceFunction<RosMessage, RosMessage> callback(ServiceFunction<RosMessage, RosMessage> cb)
        {
            _callback = cb;
            return _callback;
        }

        public virtual MReq deserialize<MReq, MRes>(ServiceCallbackHelperParams<MReq, MRes> parms) where MReq : RosMessage where MRes : RosMessage
        {
            //Logger.LogDebug("ISubscriptionCallbackHelper: deserialize");
            RosMessage msg = ROS.MakeMessage(type);
            assignSubscriptionConnectionHeader(ref msg, parms.connection_header);
            MReq t = (MReq) msg;
            t.Deserialize(parms.response.Serialized);
            return t;
            //return SerializationHelper.Deserialize<T>(parms.buffer);
        }

        private void assignSubscriptionConnectionHeader(ref RosMessage msg, IDictionary<string, string> p)
        {
            // Logger.LogDebug("ISubscriptionCallbackHelper: assignSubscriptionConnectionHeader");
            msg.connection_header = new Dictionary<string, string>(p);
        }
    }
}