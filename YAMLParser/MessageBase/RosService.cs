using System;

namespace Uml.Robotics.Ros
{
    public delegate RosMessage RosServiceDelegate(RosMessage request);

    public class RosService
    {
        public virtual string MD5Sum() { return ""; }

        public virtual string ServiceDefinition() { return ""; }

        public virtual string ServiceType { get { return "undefined/unknown"; } }

        public string msgtype_req
        {
            get { return RequestMessage.MessageType; }
        }

        public string msgtype_res
        {
            get { return ResponseMessage.MessageType; }
        }

        public RosMessage RequestMessage, ResponseMessage;

        protected RosMessage GeneralInvoke(RosServiceDelegate invocation, RosMessage m)
        {
            return invocation.Invoke(m);
        }

        public RosService()
        {
        }

        protected void InitSubtypes(RosMessage request, RosMessage response)
        {
            RequestMessage = request;
            ResponseMessage = response;
        }
    }
}
