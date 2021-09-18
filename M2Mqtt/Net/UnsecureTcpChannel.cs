/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System;
using System.Net;
using System.Net.Sockets;

namespace uPLibrary.Networking.M2Mqtt {
    /// <summary>
    /// Channel to communicate over the network
    /// </summary>
    public class UnsecureTcpChannel : IMqttNetworkChannel {
        private Socket _socket;

        public string RemoteHostName { get; private set; }
        public IPAddress RemoteIpAddress { get; private set; }
        public int RemotePort { get; private set; }

        /// <summary>
        /// Data available on the channel
        /// </summary>
        public bool DataAvailable {
            get {
                return _socket.Available > 0;
            }
        }

        public UnsecureTcpChannel(Socket socket) {
            _socket = socket;
        }

        public UnsecureTcpChannel(string remoteHostName, int remotePort) {
            IPAddress remoteIpAddress = null;
            try {
                // check if remoteHostName is a valid IP address and get it
                remoteIpAddress = IPAddress.Parse(remoteHostName);
            }
            catch {
            }

            // in this case the parameter remoteHostName isn't a valid IP address
            if (remoteIpAddress == null) {
                var hostEntry = Dns.GetHostEntryAsync(remoteHostName).Result;

                if ((hostEntry != null) && (hostEntry.AddressList.Length > 0)) {
                    // check for the first address not null
                    // it seems that with .Net Micro Framework, the IPV6 addresses aren't supported and return "null"
                    var i = 0;
                    while (hostEntry.AddressList[i] == null) {
                        i++;
                    }

                    remoteIpAddress = hostEntry.AddressList[i];
                }
                else {
                    throw new Exception("No address found for the remote host name");
                }
            }

            RemoteHostName = remoteHostName;
            RemoteIpAddress = remoteIpAddress;
            RemotePort = remotePort;
        }

        /// <summary>
        /// Connect to remote server
        /// </summary>
        public void Connect() {
            _socket = new Socket(RemoteIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // try connection to the broker
            _socket.Connect(RemoteHostName, RemotePort);

        }

        /// <summary>
        /// Send data on the network channel
        /// </summary>
        /// <param name="buffer">Data buffer to send</param>
        /// <returns>Number of byte sent</returns>
        public int Send(byte[] buffer) {
            return _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public bool TrySend(byte[] buffer) {
            var isSent = false;

            try {
                Send(buffer);
                isSent = true;
            }
            catch (Exception) {
#warning maybe need to to some specific exception handling?

                //if (typeof(SocketException) == e.GetType()) {
                //    // connection reset by broker
                //    if (((SocketException)e).SocketErrorCode == SocketError.ConnectionReset) {
                //        IsConnected = false;
                //    }
                //}
            }

            return isSent;
        }

        /// <summary>
        /// Receive data from the network
        /// </summary>
        /// <param name="buffer">Data buffer for receiving data</param>
        /// <returns>Number of bytes received</returns>
        public int Receive(byte[] buffer) {
            // read all data needed (until fill buffer)
            var idx = 0;
            while (idx < buffer.Length) {
                // fixed scenario with socket closed gracefully by peer/broker and
                // Read return 0. Avoid infinite loop.
                var read = _socket.Receive(buffer, idx, buffer.Length - idx, SocketFlags.None);
                if (read == 0) {
                    return 0;
                }

                idx += read;
            }
            return buffer.Length;
        }

        /// <summary>
        /// Receive data from the network channel with a specified timeout
        /// </summary>
        /// <param name="buffer">Data buffer for receiving data</param>
        /// <param name="timeout">Timeout on receiving (in milliseconds)</param>
        /// <returns>Number of bytes received</returns>
        public int Receive(byte[] buffer, int timeout) {
            // check data availability (timeout is in microseconds)
            if (_socket.Poll(timeout * 1000, SelectMode.SelectRead)) {
                return Receive(buffer);
            }
            else {
                return 0;
            }
        }

        /// <summary>
        /// Close the network channel
        /// </summary>
        public void Close() {
            try {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch {
                // An error occurred when attempting to access the socket or socket has been closed
                // Refer to: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown(v=vs.110).aspx
            }
            _socket.Dispose();
        }

        /// <summary>
        /// Accept connection from a remote client
        /// </summary>
        public void Accept() {
            return;
        }
    }
}
