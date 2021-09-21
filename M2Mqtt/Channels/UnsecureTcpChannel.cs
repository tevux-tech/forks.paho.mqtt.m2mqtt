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

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Unsecure channel to communicate over the network.
    /// </summary>
    internal class UnsecureTcpChannel : IMqttNetworkChannel {
        private Socket _socket;

        public string RemoteHostName { get; private set; }
        public IPAddress RemoteIpAddress { get; private set; }
        public int RemotePort { get; private set; }

        public bool DataAvailable {
            get {
                return _socket.Available > 0;
            }
        }

        public bool IsConnected { get; private set; }

        public UnsecureTcpChannel() {

        }

        public bool TryConnect(string remoteHostName, ushort remotePort) {
            bool isOk;

            if (IPAddress.TryParse(remoteHostName, out var remoteIpAddress)) {
                // Hostname is actually a valid IP address.
                RemoteIpAddress = remoteIpAddress;
                isOk = true;
            }
            else {
                // Maybe it is a valid hostname? We can get IP address from DNS cache then.
                var hostEntry = Dns.GetHostEntryAsync(remoteHostName).Result;

                if ((hostEntry != null) && (hostEntry.AddressList.Length > 0)) {
                    // Check for the first address not null.
                    var i = 0;
                    while (hostEntry.AddressList[i] == null) {
                        i++;
                    }

                    RemoteIpAddress = hostEntry.AddressList[i];
                    isOk = true;
                }
                else {
                    isOk = false;
                }
            }

            RemoteHostName = remoteHostName;
            RemotePort = remotePort;

            if (isOk) {
                try {
                    _socket = new Socket(RemoteIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    // try connection to the broker
                    _socket.Connect(RemoteHostName, RemotePort);

                    isOk = true;
                    IsConnected = true;
                }
                catch {
                    isOk = false;
                    Close();
                }
            }

            return isOk;
        }

        public bool TrySend(byte[] buffer) {
            bool isSent;

            try {
                _ = _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                isSent = true;
            }
            catch (Exception) {
                isSent = false;
                Close();
            }

            return isSent;
        }

        /// <summary>
        /// Read channel until provided buffer is full, or some error occurs.
        /// </summary>
        public bool TryReceive(byte[] buffer) {
            var isSocketAlright = true;
            var idx = 0;
            while ((idx < buffer.Length) && isSocketAlright) {
                var bytesReceived = 0;
                try {
                    bytesReceived = _socket.Receive(buffer, idx, buffer.Length - idx, SocketFlags.None);
                }
                catch (Exception) {
                    isSocketAlright = false;
                }

                if (bytesReceived == 0) {
                    // Socket closed gracefully by peer / broker.
                    isSocketAlright = false;
                }

                idx += bytesReceived;
            }

            if (isSocketAlright == false) { Close(); }

            return isSocketAlright;
        }

        public void Close() {
            IsConnected = false;

            try {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch {
                // An error occurred when attempting to access the socket or socket has been closed
                // Refer to: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown(v=vs.110).aspx
            }

            _socket.Dispose();
        }
    }
}
