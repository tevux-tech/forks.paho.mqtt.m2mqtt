﻿/*
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
   Simonas Greicius - 2021 rework
*/

using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Secure channel to communicate over the network.
    /// </summary>
    public class SecureTcpChannel : IMqttNetworkChannel {
        private Socket _socket;
        private readonly ChannelConnectionOptions _connectionOptions;
        private SslStream _sslStream;
        private NetworkStream _netStream;
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public bool DataAvailable {
            get {
                return _netStream.DataAvailable;
            }
        }

        public bool IsConnected { get; private set; }

        public SecureTcpChannel(ChannelConnectionOptions connectionOptions) {
            _connectionOptions = connectionOptions;
        }

        public bool TryConnect() {
            bool isOk;

            if (IPAddress.TryParse(_connectionOptions.Hostname, out var remoteIpAddress)) {
                // Hostname is actually a valid IP address.
                isOk = true;
            }
            else {
                // Maybe it is a valid hostname? We can get IP address from DNS cache then.
                var hostEntry = Dns.GetHostEntryAsync(_connectionOptions.Hostname).Result;

                if ((hostEntry != null) && (hostEntry.AddressList.Length > 0)) {
                    // Check for the first address not null.
                    var i = 0;
                    while (hostEntry.AddressList[i] == null) {
                        i++;
                    }

                    remoteIpAddress = hostEntry.AddressList[i];
                    isOk = true;
                }
                else {
                    _log.Error($"Cannot determine IP address from hostname {_connectionOptions.Hostname}. Even DNS cannot help.");
                    isOk = false;
                }
            }

            if (isOk) {
                try {
                    _socket = new Socket(remoteIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(remoteIpAddress, _connectionOptions.Port);

                    _netStream = new NetworkStream(_socket);
                    _sslStream = new SslStream(_netStream, false, _connectionOptions.UserCertificateValidationCallback, _connectionOptions.UserCertificateSelectionCallback);

                    var clientCertificates = new X509CertificateCollection(new X509Certificate[] { _connectionOptions.Certificate });

                    _sslStream.AuthenticateAsClient(_connectionOptions.Hostname, clientCertificates, false);

                    isOk = true;
                    IsConnected = true;
                }
                catch (Exception ex) {
                    _log.Error(ex, $"Cannot open socket to {_connectionOptions.Hostname}.");
                    isOk = false;
                    Close();
                }
            }

            return isOk;
        }

        public bool TrySend(byte[] buffer) {
            bool isSent;

            try {
                _sslStream.Write(buffer, 0, buffer.Length);
                _sslStream.Flush();

                isSent = true;
            }
            catch (Exception ex) {
                _log.Error(ex, $"Cannot send data over socket.");
                isSent = false;
                Close();
            }

            return isSent;
        }


        public bool TryReceive(byte[] buffer) {
            var isSocketAlright = true;
            var idx = 0;
            while ((idx < buffer.Length) && isSocketAlright) {
                var bytesReceived = 0;
                try {
                    bytesReceived = _sslStream.Read(buffer, idx, buffer.Length - idx);
                }
                catch (Exception ex) {
                    _log.Error(ex, $"Cannot receive data from socket.");
                    isSocketAlright = false;
                }

                if (bytesReceived == 0) {
                    _log.Error($"Socket closed gracefully by peer / broker..");
                    isSocketAlright = false;
                }

                idx += bytesReceived;
            }

            if (isSocketAlright == false) { Close(); }

            return isSocketAlright;
        }


        public void Close() {
            IsConnected = false;

            _netStream.Flush();
            _sslStream.Flush();

            try {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex) {
                _log.Error(ex, $"An error occurred when attempting to access the socket or socket has been closed.");
                // An error occurred when attempting to access the socket or socket has been closed
                // Refer to: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown(v=vs.110).aspx
            }
            _socket.Dispose();
        }
    }
}
