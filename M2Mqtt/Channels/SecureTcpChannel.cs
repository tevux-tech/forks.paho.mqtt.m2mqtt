///*
//Copyright (c) 2013, 2014 Paolo Patierno

//All rights reserved. This program and the accompanying materials
//are made available under the terms of the Eclipse Public License v1.0
//and Eclipse Distribution License v1.0 which accompany this distribution. 

//The Eclipse Public License is available at 
//   http://www.eclipse.org/legal/epl-v10.html
//and the Eclipse Distribution License is available at 
//   http://www.eclipse.org/org/documents/edl-v10.php.

//Contributors:
//   Paolo Patierno - initial API and implementation and/or initial documentation
//*/

//using System.Net.Security;
//using System.Security.Authentication;
//using System.Net.Sockets;
//using System.Net;
//using System.Security.Cryptography.X509Certificates;
//using System;
//using System.Collections.Generic;

//namespace Tevux.Protocols.Mqtt {
//    /// <summary>
//    /// Secure channel to communicate over the network.
//    /// </summary>
//    public class SecureTcpChannel : IMqttNetworkChannel {
//        private readonly RemoteCertificateValidationCallback _userCertificateValidationCallback;
//        private readonly LocalCertificateSelectionCallback _userCertificateSelectionCallback;

//        private Socket _socket;

//        private readonly X509Certificate _clientCert;
//        private readonly MqttSslProtocols _sslProtocol;

//        public string RemoteHostName { get; private set; }
//        public IPAddress RemoteIpAddress { get; private set; }
//        public int RemotePort { get; private set; }

//        private SslStream _sslStream;
//        private NetworkStream _netStream;

//        private readonly List<string> _alpnProtocols = new List<string>();

//        public bool DataAvailable {
//            get {
//                return _netStream.DataAvailable;
//            }
//        }

//        public SecureTcpChannel(Socket socket, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback) {
//            _socket = socket;
//            _sslProtocol = sslProtocol;
//            _userCertificateValidationCallback = userCertificateValidationCallback;
//            _userCertificateSelectionCallback = userCertificateSelectionCallback;
//        }

//        public SecureTcpChannel(string remoteHostName, int remotePort, X509Certificate clientCert, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback, List<string> alpnProtocols = null) {
//            IPAddress remoteIpAddress = null;
//            try {
//                // check if remoteHostName is a valid IP address and get it
//                remoteIpAddress = IPAddress.Parse(remoteHostName);
//            }
//            catch {
//            }

//            // in this case the parameter remoteHostName isn't a valid IP address
//            if (remoteIpAddress == null) {
//                var hostEntry = Dns.GetHostEntryAsync(remoteHostName).Result;

//                if ((hostEntry != null) && (hostEntry.AddressList.Length > 0)) {
//                    // check for the first address not null
//                    // it seems that with .Net Micro Framework, the IPV6 addresses aren't supported and return "null"
//                    var i = 0;
//                    while (hostEntry.AddressList[i] == null) {
//                        i++;
//                    }

//                    remoteIpAddress = hostEntry.AddressList[i];
//                }
//                else {
//                    throw new Exception("No address found for the remote host name");
//                }
//            }

//            RemoteHostName = remoteHostName;
//            RemoteIpAddress = remoteIpAddress;
//            RemotePort = remotePort;
//            _clientCert = clientCert;
//            _sslProtocol = sslProtocol;
//            _userCertificateValidationCallback = userCertificateValidationCallback;
//            _userCertificateSelectionCallback = userCertificateSelectionCallback;

//            if (alpnProtocols != null) {
//                _alpnProtocols = alpnProtocols;
//            }
//        }

//        public void Connect() {
//            _socket = new Socket(RemoteIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
//            // try connection to the broker
//            _socket.Connect(RemoteHostName, RemotePort);

//            // create SSL stream
//            _netStream = new NetworkStream(_socket);
//            _sslStream = new SslStream(_netStream, false, _userCertificateValidationCallback, _userCertificateSelectionCallback);

//            // server authentication (SSL/TLS handshake)
//            X509CertificateCollection clientCertificates = null;
//            // check if there is a client certificate to add to the collection, otherwise it's null (as empty)
//            if (_clientCert != null) {
//                clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCert });
//            }

//#if NETCOREAPP3_1
//            if (_alpnProtocols.Count > 0) {
//                _sslStream = new SslStream(_netStream, false);
//                var authOptions = new SslClientAuthenticationOptions();
//                var sslProtocolList = new List<SslApplicationProtocol>();
//                foreach (var alpnProtocol in _alpnProtocols) {
//                    sslProtocolList.Add(new SslApplicationProtocol(alpnProtocol));
//                }
//                authOptions.ApplicationProtocols = sslProtocolList;
//                authOptions.EnabledSslProtocols = MqttSslUtility.ToSslPlatformEnum(_sslProtocol);
//                authOptions.TargetHost = RemoteHostName;
//                authOptions.AllowRenegotiation = false;
//                authOptions.ClientCertificates = clientCertificates;
//                authOptions.EncryptionPolicy = EncryptionPolicy.RequireEncryption;

//                _sslStream.AuthenticateAsClientAsync(authOptions).Wait();
//            }
//            else {
//                _sslStream.AuthenticateAsClientAsync(RemoteHostName, clientCertificates, MqttSslUtility.ToSslPlatformEnum(_sslProtocol), false).Wait();
//            }
//#else
//                _sslStream.AuthenticateAsClientAsync(RemoteHostName, clientCertificates, MqttSslUtility.ToSslPlatformEnum(_sslProtocol), false).Wait();
//#endif
//        }

//        public int Send(byte[] buffer) {
//            _sslStream.Write(buffer, 0, buffer.Length);
//            _sslStream.Flush();
//            return buffer.Length;
//        }

//        public bool TrySend(byte[] buffer) {
//            var isSent = false;

//            try {
//                Send(buffer);
//                isSent = true;
//            }
//            catch (Exception) {
//#warning maybe need to to some specific exception handling?

//                //if (typeof(SocketException) == e.GetType()) {
//                //    // connection reset by broker
//                //    if (((SocketException)e).SocketErrorCode == SocketError.ConnectionReset) {
//                //        IsConnected = false;
//                //    }
//                //}
//            }

//            return isSent;
//        }

//        public int TryReceive(byte[] buffer) {
//            // read all data needed (until fill buffer)
//            var idx = 0;
//            while (idx < buffer.Length) {
//                // fixed scenario with socket closed gracefully by peer/broker and
//                // Read return 0. Avoid infinite loop.
//                var read = _sslStream.Read(buffer, idx, buffer.Length - idx);
//                if (read == 0) {
//                    return 0;
//                }

//                idx += read;
//            }
//            return buffer.Length;
//        }

//        public int Receive(byte[] buffer, int timeout) {
//            // check data availability (timeout is in microseconds)
//            if (_socket.Poll(timeout * 1000, SelectMode.SelectRead)) {
//                return TryReceive(buffer);
//            }
//            else {
//                return 0;
//            }
//        }

//        public void Close() {
//            _netStream.Flush();
//            _sslStream.Flush();

//            try {
//                _socket.Shutdown(SocketShutdown.Both);
//            }
//            catch {
//                // An error occurred when attempting to access the socket or socket has been closed
//                // Refer to: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown(v=vs.110).aspx
//            }
//            _socket.Dispose();
//        }
//    }

//    public static class MqttSslUtility {
//        public static SslProtocols ToSslPlatformEnum(MqttSslProtocols mqttSslProtocol) {
//            switch (mqttSslProtocol) {
//                case MqttSslProtocols.None:
//                    return SslProtocols.None;

//                case MqttSslProtocols.SSLv3:
//                    throw new ArgumentException("Ssl3 is obsolete. It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202");

//                case MqttSslProtocols.TLSv1_0:
//                    return SslProtocols.Tls;

//                case MqttSslProtocols.TLSv1_1:
//                    return SslProtocols.Tls11;

//                case MqttSslProtocols.TLSv1_2:
//                    return SslProtocols.Tls12;

//                default:
//                    throw new ArgumentException("SSL/TLS protocol version not supported");
//            }
//        }
//    }
//}
