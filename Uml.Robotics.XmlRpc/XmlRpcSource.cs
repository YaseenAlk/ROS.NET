﻿using System;
using System.Net.Sockets;
using System.Text;

namespace Uml.Robotics.XmlRpc
{
    public abstract class XmlRpcSource : IDisposable
    {
        private const int READ_BUFFER_LENGTH = 4096;

        private bool _deleteOnClose;

        // In the client, keep connections open if you intend to make multiple calls.
        private bool _keepOpen;

        public bool KeepOpen
        {
            get { return _keepOpen; }
            set { _keepOpen = value; }
        }

        public virtual NetworkStream getStream()
        {
            return null;
        }

        public virtual Socket getSocket()
        {
            return null;
        }

        public virtual void Close()
        {
            throw new NotImplementedException();
        }

        public virtual XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            throw new NotImplementedException();
        }

        //! Return whether the file descriptor should be kept open if it is no longer monitored.
        public bool getKeepOpen()
        {
            return _keepOpen;
        }

        //! Specify whether the file descriptor should be kept open if it is no longer monitored.
        public void setKeepOpen(bool b = true)
        {
            _keepOpen = b;
        }

        internal virtual bool readHeader(ref HttpHeader header)
        {
            // Read available data
            int dataLen = 0;
            var stream = getStream();
            if (stream == null)
            {
                throw new Exception("Could not access network stream");
            }
            byte[] data = new byte[READ_BUFFER_LENGTH];
            try
            {
                dataLen = stream.Read(data, 0, READ_BUFFER_LENGTH);

                if (dataLen == 0)
                    return false; // If it is disconnect

                if (header == null)
                {
                    header = new HttpHeader(Encoding.ASCII.GetString(data, 0, dataLen));
                    if (header.HeaderStatus == HttpHeader.STATUS.UNINITIALIZED)
                        return false; //should only happen if the constructor's invocation of Append did not happen as desired
                }
                else if (header.Append(Encoding.ASCII.GetString(data, 0, dataLen)) == HttpHeader.STATUS.PARTIAL_HEADER)
                    return true; //if we successfully append a piece of the header, return true, but DO NOT change states
            }
            catch (SocketException ex)
            {
                XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).", ex.Message);
                return false;
            }

            if (header.HeaderStatus != HttpHeader.STATUS.COMPLETE_HEADER)
                return false;

            return true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        // In the server, a new source (XmlRpcServerConnection) is created
        // for each connected client. When each connection is closed, the
        // corresponding source object is deleted.
    }
}
