﻿using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace Uml.Robotics.XmlRpc
{
    public class XmlRpcClient : XmlRpcSource
    {
        // Static data
        const string REQUEST_BEGIN = "<?xml version=\"1.0\"?>\r\n<methodCall><methodName>";
        const string REQUEST_END_METHODNAME = "</methodName>\r\n";
        const string PARAMS_TAG = "<params>";
        const string PARAMS_ETAG = "</params>";
        const string PARAM_TAG = "<param>";
        const string PARAM_ETAG = "</param>";
        const string REQUEST_END = "</methodCall>\r\n";

        public string HostUri = "";
        private int _bytesWritten;
        private ConnectionState _connectionState;

        // Event dispatcher
        private XmlRpcDispatch _disp = new XmlRpcDispatch();
        private bool _eof;
        private bool _executing;
        private string _host;
        private int _port;
        private string _request;

        // Number of times the client has attempted to send the request
        private int _sendAttempts;
        private string _uri;
        private HttpHeader header;
        private TcpClient socket;
        public delegate void DisposedEvent();
        public event DisposedEvent Disposed;

        public XmlRpcClient(string hostName, int port, string path)
        {
            Initialize(hostName, port, path);
        }

        public XmlRpcClient(string hostName, int port)
            : this(hostName, port, "/")
        {
        }

        public XmlRpcClient(string uri)
        {
            if (!uri.Contains("://"))
                throw new ArgumentException("Protocol part is missing in URI argument.", nameof(uri));
            uri = uri.Remove(0, uri.IndexOf("://") + 3);
            uri.Trim('/');

            string[] chunks = uri.Split(':');
            string hn = chunks[0];
            string[] chunks2 = chunks[1].Split('/');
            int p = int.Parse(chunks2[0]);
            string u = "/";
            if (chunks2.Length > 1 && chunks2[1].Length != 0)
                u = chunks2[1];

            Initialize(hn, p, u);
        }

        #region public get passthroughs

        public bool IsConnected
        {
            get { return socket != null && socket.Connected; }
        }

        public string Host
        {
            get { return _host; }
        }

        public string Uri
        {
            get { return _uri; }
        }

        public int Port
        {
            get { return _port; }
        }

        public string Request
        {
            get { return _request; }
        }

        public string Header
        {
            get { return header.Header; }
        }

        public string Response
        {
            get { return header.DataString; }
        }

        public int SendAttempts
        {
            get { return _sendAttempts; }
        }

        public int BytesWritten
        {
            get { return _bytesWritten; }
        }

        public bool Executing
        {
            get { return _executing; }
        }

        public bool EOF
        {
            get { return _eof; }
        }

        public int ContentLength
        {
            get { return header.ContentLength; }
        }

        #endregion

        #region public function passthroughs

        public bool CheckIdentity(string host, int port, string uri)
        {
            return _host.Equals(host) && _port == port && _uri.Equals(uri); // checkident(instance, host, port, uri);
        }

        // Execute the named procedure on the remote server.
        // Params should be an array of the arguments for the method.
        // Returns true if the request was sent and a result received (although the result
        // might be a fault).
        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.MAX, "XmlRpcClient::Execute: method {0} (_connectionState {0}).", method, _connectionState);
            lock (this)
            {
                // This is not a thread-safe operation, if you want to do multithreading, use separate
                // clients for each thread. If you want to protect yourself from multiple threads
                // accessing the same client, replace this code with a real mutex.
                if (_executing)
                    return false;

                try
                {
                    _executing = true;
                    _sendAttempts = 0;

                    if (!setupConnection())
                        return false;

                    if (!generateRequest(method, parameters))
                        return false;

                    double msTime = -1.0;
                    _disp.Work(msTime);

                    if (_connectionState != ConnectionState.IDLE)
                        return false;

                    if (!parseResponse(result, header.DataString))
                        return false;

                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient::execute: method {0} completed.", method);
                }
                finally
                {
                    _executing = false;
                }
            }
            return true;
        }

        // Execute the named procedure on the remote server, non-blocking.
        // Params should be an array of the arguments for the method.
        // Returns true if the request was sent and a result received (although the result
        // might be a fault).
        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.MAX, "XmlRpcClient::ExecuteNonBlock: method {0} (_connectionState {0}.", method, _connectionState);

            // This is not a thread-safe operation, if you want to do multithreading, use separate
            // clients for each thread. If you want to protect yourself from multiple threads
            // accessing the same client, replace this code with a real mutex.

            XmlRpcValue result = new XmlRpcValue();
            if (_executing)
                return false;

            _executing = true;

            _sendAttempts = 0;

            if (!setupConnection())
            {
                _executing = false;
                return false;
            }

            if (!generateRequest(method, parameters))
            {
                _executing = false;
                return false;
            }

            _executing = false;
            return true;
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
            if (_connectionState != ConnectionState.IDLE)
                return false;

            parseResponse(result, header.DataString);
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient::execute: method completed.");
            return true;
        }

        #endregion

        public override NetworkStream getStream()
        {
            return socket.GetStream();
        }

        // Server location

        private string getHost()
        {
            return _host;
        }

        private string getUri()
        {
            return _uri;
        }

        private int getPort()
        {
            return _port;
        }

        // The xml-encoded request, http header of response, and response xml

        public override void Close()
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient::Close()");
            CloseInternal();
            if (Disposed != null)
                Disposed();
        }

        public override Socket getSocket()
        {
            return socket != null ? socket.Client : null;
        }

        //done and works
        private void Initialize(string host, int port, string uri /*=0*/)
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient new client: host {0}, port {1}.", host, port);

            _host = host;
            _port = port;
            if (uri != null)
                _uri = uri;
            else
                _uri = "/RPC2";

            _connectionState = ConnectionState.CONNECTING;
            _executing = false;
            _eof = false;

            if (doConnect())
            {
                _connectionState = ConnectionState.IDLE;
            }

            // Default to keeping the connection open until an explicit close is done
            this.KeepOpen = true;
        }

        // Close the owned fd
        private void CloseInternal()
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient::close.");
            _connectionState = ConnectionState.NO_CONNECTION;
            _disp.RemoveSource(this);
            _disp.Exit();
            //_disp.removeSource(this);
            //XmlRpcSource::close();
            if (socket != null)
            {
                socket.Dispose();
                socket = null;
                //reader = null;
                //writer = null;
            }
        }

        private string getSocketError()
        {
            return "UnknownError";
        }

        // Possible IO states for the connection
        // XmlRpcSource interface implementation
        // Handle server responses. Called by the event dispatcher during execute.
        public override XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            if (eventType == XmlRpcDispatch.EventType.Exception)
            {
                if (_connectionState == ConnectionState.WRITE_REQUEST && _bytesWritten == 0)
                    XmlRpcUtil.error("Error in XmlRpcClient::handleEvent: could not connect to server ({0}).",
                        getSocketError());
                else
                    XmlRpcUtil.error("Error in XmlRpcClient::handleEvent (state {0}): {1}.",
                        _connectionState, getSocketError());
                return 0;
            }

            if (_connectionState == ConnectionState.WRITE_REQUEST)
                if (! writeRequest()) return 0;

            if (_connectionState == ConnectionState.READ_HEADER)
                if (! readHeader(ref header)) return 0;

            if (_connectionState == ConnectionState.READ_RESPONSE)
                if (! readResponse()) return 0;

            // This should probably always ask for Exception events too
            return (_connectionState == ConnectionState.WRITE_REQUEST)
                ? XmlRpcDispatch.EventType.WritableEvent : XmlRpcDispatch.EventType.ReadableEvent;
        }

        internal override bool readHeader(ref HttpHeader header)
        {
            if (base.readHeader(ref header))
            {
                if (header.HeaderStatus == HttpHeader.ParseStatus.COMPLETE_HEADER)
                {
                    _connectionState = ConnectionState.READ_RESPONSE;
                }

                return true;
            }

            return false;
        }

        // Create the socket connection to the server if necessary
        private bool setupConnection()
        {
            // If an error occurred last time through, or if the server closed the connection, close our end
            if ((_connectionState != ConnectionState.NO_CONNECTION && _connectionState != ConnectionState.IDLE) || _eof)
                CloseInternal();

            _eof = false;
            if (_connectionState == ConnectionState.NO_CONNECTION)
            {
                if (!doConnect())
                    return false;
            }

            // Prepare to write the request
            _connectionState = ConnectionState.WRITE_REQUEST;
            _bytesWritten = 0;

            // Notify the dispatcher to listen on this source (calls handleEvent when the socket is writable)
            _disp.RemoveSource(this); // Make sure nothing is left over
            _disp.AddSource(this, XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception);

            return true;
        }

        // Connect to the xmlrpc server
        private bool doConnect()
        {
            if (socket != null)
                throw new InvalidOperationException("XmlRpcClient: TcpClient has already been created.");

            try
            {
                socket = new TcpClient();
                socket.ConnectAsync(_host, _port).Wait();       // wait synchronously
            }
            catch (AggregateException ex)
            {
                var socketException = ex.InnerExceptions.OfType<SocketException>().FirstOrDefault();
                if (socketException != null)
                {
                    XmlRpcUtil.error("Error in XmlRpcClient::doConnect: Could not connect to server ({0}).", socketException.Message);
                    socket.Dispose();
                    socket = null;
                    return false;
                }

                throw;
            }

            return true;
        }

        public void Shutdown()
        {
            Close();
        }

        private string generateRequestStr(string methodName, XmlRpcValue parameters)
        {
            string body = REQUEST_BEGIN;
            body += methodName;
            body += REQUEST_END_METHODNAME;

            // If params is an array, each element is a separate parameter
            if (parameters.IsValid)
            {
                body += PARAMS_TAG;
                if (parameters.Type == XmlRpcType.Array)
                {
                    for (int i = 0; i < parameters.Count; ++i)
                    {
                        body += PARAM_TAG;
                        body += parameters[i].ToXml();
                        body += PARAM_ETAG;
                    }
                }
                else
                {
                    body += PARAM_TAG;
                    body += parameters.ToXml();
                    body += PARAM_ETAG;
                }

                body += PARAMS_ETAG;
            }
            body += REQUEST_END;
            return body;
        }

        // Encode the request to call the specified method with the specified parameters into xml
        private bool generateRequest(string methodName, XmlRpcValue parameters)
        {
            string body = generateRequestStr(methodName, parameters);

            string header = generateHeader(body);
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcClient::generateRequest: header is {0} bytes, content-length is {1}.", header.Length, body.Length);

            _request = header + body;
            return true;
        }

        // Prepend http headers
        private string generateHeader(string body)
        {
            string header = "POST " + _uri + " HTTP/1.1\r\nUser-Agent: ";
            header += XmlRpcUtil.XMLRPC_VERSION;
            header += "\r\nHost: ";
            header += _host;

            string buff = String.Format(":{0}\r\n", _port);
            //sprintf(buff,":%d\r\n", _port);


            header += buff;
            header += "Content-Type: text/xml\r\nContent-length: ";
            buff = String.Format("{0}\r\n\r\n", body.Length);

            return header + buff;
        }

        private bool writeRequest()
        {
            if (_bytesWritten == 0)
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.MAX, "XmlRpcClient::writeRequest (attempt {0}):\n{1}\n", _sendAttempts + 1, _request);
            // Try to write the request
            try
            {
                if (!socket.Connected)
                    XmlRpcUtil.error("XmlRpcClient::writeRequest not connected");
                MemoryStream memstream = new MemoryStream();
                using (StreamWriter writer = new StreamWriter(memstream))
                {
                    writer.Write(_request);
                    writer.Flush();
                }
                var stream = socket.GetStream();
                try
                {
                    var buffer = new ArraySegment<byte>();
                    memstream.TryGetBuffer(out buffer);
                    stream.Write(buffer.Array, buffer.Offset, buffer.Count);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    XmlRpcUtil.error(string.Format("Exception while writing request: {0}", ex.Message));
                }
                _bytesWritten = _request.Length;
            }
            catch (IOException ex)
            {
                XmlRpcUtil.error("Error in XmlRpcClient::writeRequest: write error ({0}).", ex.Message);
                return false;
            }

            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO, "XmlRpcClient::writeRequest: wrote {0} of {1} bytes.", _bytesWritten, _request.Length);

            // Wait for the result
            if (_bytesWritten == _request.Length)
            {
                _connectionState = ConnectionState.READ_HEADER;
                header = null;
            }
            return true;
        }

        private bool readResponse()
        {
            int left = header.ContentLength - header.DataString.Length;
            int dataLen = 0;
            if (left > 0)
            {
                byte[] data = new byte[left];
                try
                {
                    var stream = socket.GetStream();
                    dataLen = stream.Read(data, 0, left);
                    if (dataLen == 0)
                    {
                        XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR, "XmlRpcClient::readResponse: Stream was closed");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    XmlRpcUtil.error("XmlRpcClient::readResponse: error while reading the rest of data ({0}).", ex.Message);
                    return false;
                }
                header.Append(Encoding.ASCII.GetString(data, 0, dataLen));
            }
            if (header.ContentComplete)
            {
                // Otherwise, parse and dispatch the request
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO, "XmlRpcClient::readResponse read {0} bytes.", _request.Length);

                _connectionState = ConnectionState.IDLE;

                return false; // no need to continue monitoring because we're done reading the response
            }

            // Continue monitoring this source
            return true;
        }

        // Convert the response xml into a result value
        private bool parseResponse(XmlRpcValue result, string response)
        {
            // Parse response xml into result
            var responseDocument = XDocument.Parse(response);
            var methodResponseElement = responseDocument.Element("methodResponse");
            if (methodResponseElement == null)
                throw new XmlRpcException("Expected <methodResponse> element missing. Response:\n" + response);

            var paramsElement = methodResponseElement.Element("params");
            var faultElement = methodResponseElement.Element("fault");

            if (paramsElement != null)
            {
                var selection = paramsElement.Elements("param").ToList();
                if (selection.Count > 1)
                {
                    result.SetArray(selection.Count);
                    for (int i = 0; i < selection.Count; i++)
                    {
                        var value = new XmlRpcValue();
                        value.FromXElement(selection[i].Element("value"));
                        result.Set(i, value);
                    }
                }
                else if (selection.Count == 1)
                {
                    result.FromXElement(selection[0].Element("value"));
                }
                else
                {
                    return false;
                }
            }
            else if (faultElement != null)
            {
                result.FromXElement(faultElement);
                return false;
            }
            else
            {
                throw new XmlRpcException("Invalid response - no param or fault tag. Response:\n" + response);
            }

            return true;
        }

        private enum ConnectionState
        {
            NO_CONNECTION,
            CONNECTING,
            WRITE_REQUEST,
            READ_HEADER,
            READ_RESPONSE,
            IDLE
        };
    }
}