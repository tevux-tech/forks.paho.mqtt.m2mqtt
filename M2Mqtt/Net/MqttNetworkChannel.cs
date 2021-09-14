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

using System.Net.Security;
using System.Security.Authentication;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;

namespace uPLibrary.Networking.M2Mqtt {
    /// <summary>
    /// Channel to communicate over the network
    /// </summary>
    public class MqttNetworkChannel : IMqttNetworkChannel {
        private readonly RemoteCertificateValidationCallback _userCertificateValidationCallback;
        private readonly LocalCertificateSelectionCallback _userCertificateSelectionCallback;

        private Socket _socket;
        private bool _secure;

        // CA certificate (on client)
        private X509Certificate _caCert;
        // Server certificate (on broker)
        private X509Certificate _serverCert;
        // client certificate (on client)
        private X509Certificate _clientCert;

        // SSL/TLS protocol version
        private MqttSslProtocols _sslProtocol;

        public string RemoteHostName { get; private set; }
        public IPAddress RemoteIpAddress { get; private set; }
        public int RemotePort { get; private set; }

        // SSL stream
        private SslStream _sslStream;
        private NetworkStream _netStream;

        /// <summary>
        /// List of Protocol for ALPN
        /// </summary>
        private List<string> _alpnProtocols = new List<string>();


        /// <summary>
        /// Data available on the channel
        /// </summary>
        public bool DataAvailable {
            get {
                if (_secure) {
                    return _netStream.DataAvailable;
                }
                else {
                    return (_socket.Available > 0);
                }
            }
        }

        public MqttNetworkChannel(Socket socket) : this(socket, false, null, MqttSslProtocols.None, null, null) {

        }

        public MqttNetworkChannel(Socket socket, bool secure, X509Certificate serverCert, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback) {
            _socket = socket;
            _secure = secure;
            _serverCert = serverCert;
            _sslProtocol = sslProtocol;
            this._userCertificateValidationCallback = userCertificateValidationCallback;
            this._userCertificateSelectionCallback = userCertificateSelectionCallback;
        }

        public MqttNetworkChannel(string remoteHostName, int remotePort) : this(remoteHostName, remotePort, false, null, null, MqttSslProtocols.None, null, null) {
        }

        public MqttNetworkChannel(string remoteHostName, int remotePort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback, List<string> alpnProtocols = null) {
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
            _secure = secure;
            _caCert = caCert;
            _clientCert = clientCert;
            _sslProtocol = sslProtocol;
            this._userCertificateValidationCallback = userCertificateValidationCallback;
            this._userCertificateSelectionCallback = userCertificateSelectionCallback;

            if (alpnProtocols != null) {
                _alpnProtocols = alpnProtocols;
            }
        }

        /// <summary>
        /// Connect to remote server
        /// </summary>
        public void Connect() {
            _socket = new Socket(RemoteIpAddress.GetAddressFamily(), SocketType.Stream, ProtocolType.Tcp);
            // try connection to the broker
            _socket.Connect(RemoteHostName, RemotePort);

            // secure channel requested
            if (_secure) {
                // create SSL stream
                _netStream = new NetworkStream(_socket);
                _sslStream = new SslStream(_netStream, false, _userCertificateValidationCallback, _userCertificateSelectionCallback);

                // server authentication (SSL/TLS handshake)
                X509CertificateCollection clientCertificates = null;
                // check if there is a client certificate to add to the collection, otherwise it's null (as empty)
                if (_clientCert != null) {
                    clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCert });
                }

#if NETCOREAPP3_1
                if (_alpnProtocols.Count > 0) {
                    _sslStream = new SslStream(_netStream, false);
                    var authOptions = new SslClientAuthenticationOptions();
                    var sslProtocolList = new List<SslApplicationProtocol>();
                    foreach (var alpnProtocol in _alpnProtocols) {
                        sslProtocolList.Add(new SslApplicationProtocol(alpnProtocol));
                    }
                    authOptions.ApplicationProtocols = sslProtocolList;
                    authOptions.EnabledSslProtocols = MqttSslUtility.ToSslPlatformEnum(_sslProtocol);
                    authOptions.TargetHost = RemoteHostName;
                    authOptions.AllowRenegotiation = false;
                    authOptions.ClientCertificates = clientCertificates;
                    authOptions.EncryptionPolicy = EncryptionPolicy.RequireEncryption;

                    _sslStream.AuthenticateAsClientAsync(authOptions).Wait();
                }
                else {
                    _sslStream.AuthenticateAsClientAsync(RemoteHostName, clientCertificates, MqttSslUtility.ToSslPlatformEnum(_sslProtocol), false).Wait();
                }
#else
                _sslStream.AuthenticateAsClientAsync(RemoteHostName, clientCertificates, MqttSslUtility.ToSslPlatformEnum(_sslProtocol), false).Wait();
#endif
            }
        }

        /// <summary>
        /// Send data on the network channel
        /// </summary>
        /// <param name="buffer">Data buffer to send</param>
        /// <returns>Number of byte sent</returns>
        public int Send(byte[] buffer) {
            if (_secure) {
                _sslStream.Write(buffer, 0, buffer.Length);
                _sslStream.Flush();
                return buffer.Length;
            }
            else {
                return _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// Receive data from the network
        /// </summary>
        /// <param name="buffer">Data buffer for receiving data</param>
        /// <returns>Number of bytes received</returns>
        public int Receive(byte[] buffer) {
            if (_secure) {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < buffer.Length) {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = _sslStream.Read(buffer, idx, buffer.Length - idx);
                    if (read == 0) {
                        return 0;
                    }

                    idx += read;
                }
                return buffer.Length;
            }
            else {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < buffer.Length) {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = _socket.Receive(buffer, idx, buffer.Length - idx, SocketFlags.None);
                    if (read == 0) {
                        return 0;
                    }

                    idx += read;
                }
                return buffer.Length;
            }
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
            if (_secure) {
                _netStream.Flush();
                _sslStream.Flush();

            }

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
            // secure channel requested
            if (_secure) {
                _netStream = new NetworkStream(_socket);
                _sslStream = new SslStream(_netStream, false, _userCertificateValidationCallback, _userCertificateSelectionCallback);
                _sslStream.AuthenticateAsServerAsync(_serverCert, false, MqttSslUtility.ToSslPlatformEnum(_sslProtocol), false).Wait();
            }

            return;
        }
    }

    /// <summary>
    /// IPAddress Utility class
    /// </summary>
    public static class IPAddressUtility {
        /// <summary>
        /// Return AddressFamily for the IP address
        /// </summary>
        /// <param name="ipAddress">IP address to check</param>
        /// <returns>Address family</returns>
        public static AddressFamily GetAddressFamily(this IPAddress ipAddress) {
            return ipAddress.AddressFamily;
        }
    }

    /// <summary>
    /// MQTT SSL utility class
    /// </summary>
    public static class MqttSslUtility {
        public static SslProtocols ToSslPlatformEnum(MqttSslProtocols mqttSslProtocol) {
            switch (mqttSslProtocol) {
                case MqttSslProtocols.None:
                    return SslProtocols.None;
                // CS0618: 'SslProtocols.Ssl3' is obsolete: 'This value has been deprecated.  It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202'
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
