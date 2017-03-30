﻿namespace Uml.Robotics.XmlRpc
{
    public delegate void XmlRpcFunc(XmlRpcValue parms, XmlRpcValue reseseses);

    public class XmlRpcServerMethod
    {
        public string name;
        public XmlRpcServer server;
        private XmlRpcFunc func;

        public XmlRpcServerMethod(string functionName, XmlRpcFunc func, XmlRpcServer server)
        {
            name = functionName;
            this.server = server;
            this.func = func;
            if (server != null)
                server.AddMethod(this);
        }

        public XmlRpcFunc Func
        {
            get { return func; }
            set { func = value; }
        }

        public void Execute(XmlRpcValue parms, XmlRpcValue reseseses)
        {
            func(parms, reseseses);
        }

        public virtual string Help()
        {
            return "no help";
        }
    }
}
