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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Secure channel to communicate over the network.
    /// </summary>
    public class SecureTcpChannel : IMqttNetworkChannel {
        private Socket _socket;
        private ChannelConnectionOptions _connectionOptions;
        private SslStream _sslStream;
        private NetworkStream _netStream;

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
                _sslStream.Write(buffer, 0, buffer.Length);
                _sslStream.Flush();

                isSent = true;
            }
            catch (Exception) {
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

            _netStream.Flush();
            _sslStream.Flush();

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

    public static class MqttSslUtility {
        public static SslProtocols ToSslPlatformEnum(MqttSslProtocols mqttSslProtocol) {
            switch (mqttSslProtocol) {
                case MqttSslProtocols.None:
                    return SslProtocols.None;

                case MqttSslProtocols.SSLv3:
                    throw new ArgumentException("Ssl3 is obsolete. It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202");

                case MqttSslProtocols.TLSv1_0:
                    return SslProtocols.Tls;

                case MqttSslProtocols.TLSv1_1:
                    return SslProtocols.Tls11;

                case MqttSslProtocols.TLSv1_2:
                    return SslProtocols.Tls12;

                default:
                    throw new ArgumentException("SSL/TLS protocol version not supported");
            }
        }
    }
}
