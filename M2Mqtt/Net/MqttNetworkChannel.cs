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
*/

using System.Net.Security;
using System.Security.Authentication;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace uPLibrary.Networking.M2Mqtt
{
    /// <summary>
    /// Channel to communicate over the network
    /// </summary>
    public class MqttNetworkChannel : IMqttNetworkChannel
    {
        private readonly RemoteCertificateValidationCallback userCertificateValidationCallback;
        private readonly LocalCertificateSelectionCallback userCertificateSelectionCallback;

        // remote host information
        private string remoteHostName;
        private IPAddress remoteIpAddress;
        private int remotePort;

        // socket for communication
        private Socket socket;
        // using SSL
        private bool secure;

        // CA certificate (on client)
        private X509Certificate caCert;
        // Server certificate (on broker)
        private X509Certificate serverCert;
        // client certificate (on client)
        private X509Certificate clientCert;

        // SSL/TLS protocol version
        private MqttSslProtocols sslProtocol;

        /// <summary>
        /// Remote host name
        /// </summary>
        public string RemoteHostName { get { return remoteHostName; } }

        /// <summary>
        /// Remote IP address
        /// </summary>
        public IPAddress RemoteIpAddress { get { return remoteIpAddress; } }

        /// <summary>
        /// Remote port
        /// </summary>
        public int RemotePort { get { return remotePort; } }

        // SSL stream
        private SslStream sslStream;
        private NetworkStream netStream;

        /// <summary>
        /// List of Protocol for ALPN
        /// </summary>
        private List<string> alpnProtocols;


        /// <summary>
        /// Data available on the channel
        /// </summary>
        public bool DataAvailable
        {
            get
            {
                if (secure)
                    return netStream.DataAvailable;
                else
                    return (socket.Available > 0);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">Socket opened with the client</param>
        public MqttNetworkChannel(Socket socket)
            : this(socket, false, null, MqttSslProtocols.None, null, null)

        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">Socket opened with the client</param>
        /// <param name="secure">Secure connection (SSL/TLS)</param>
        /// <param name="serverCert">Server X509 certificate for secure connection</param>
        /// <param name="sslProtocol">SSL/TLS protocol version</param>
        /// <param name="userCertificateSelectionCallback">A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party</param>
        /// <param name="userCertificateValidationCallback">A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication</param>
        public MqttNetworkChannel(Socket socket, bool secure, X509Certificate serverCert, MqttSslProtocols sslProtocol,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback)
        {
            this.socket = socket;
            this.secure = secure;
            this.serverCert = serverCert;
            this.sslProtocol = sslProtocol;
            this.userCertificateValidationCallback = userCertificateValidationCallback;
            this.userCertificateSelectionCallback = userCertificateSelectionCallback;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="remoteHostName">Remote Host name</param>
        /// <param name="remotePort">Remote port</param>
        public MqttNetworkChannel(string remoteHostName, int remotePort)
            : this(remoteHostName, remotePort, false, null, null, MqttSslProtocols.None, null, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="remoteHostName">Remote Host name</param>
        /// <param name="remotePort">Remote port</param>
        /// <param name="secure">Using SSL</param>
        /// <param name="caCert">CA certificate</param>
        /// <param name="clientCert">Client certificate</param>
        /// <param name="sslProtocol">SSL/TLS protocol version</param>

        /// <param name="userCertificateSelectionCallback">A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party</param>
        /// <param name="userCertificateValidationCallback">A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication</param>
        public MqttNetworkChannel(string remoteHostName, int remotePort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback,
            List<string> alpnProtocols = null)
        {
            IPAddress remoteIpAddress = null;
            try
            {
                // check if remoteHostName is a valid IP address and get it
                remoteIpAddress = IPAddress.Parse(remoteHostName);
            }
            catch
            {
            }

            // in this case the parameter remoteHostName isn't a valid IP address
            if (remoteIpAddress == null)
            {
                IPHostEntry hostEntry = Dns.GetHostEntryAsync(remoteHostName).Result;

                if ((hostEntry != null) && (hostEntry.AddressList.Length > 0))
                {
                    // check for the first address not null
                    // it seems that with .Net Micro Framework, the IPV6 addresses aren't supported and return "null"
                    int i = 0;
                    while (hostEntry.AddressList[i] == null)
                        i++;
                    remoteIpAddress = hostEntry.AddressList[i];
                }
                else
                {
                    throw new Exception("No address found for the remote host name");
                }
            }

            this.remoteHostName = remoteHostName;
            this.remoteIpAddress = remoteIpAddress;
            this.remotePort = remotePort;
            this.secure = secure;
            this.caCert = caCert;
            this.clientCert = clientCert;
            this.sslProtocol = sslProtocol;
            this.userCertificateValidationCallback = userCertificateValidationCallback;
            this.userCertificateSelectionCallback = userCertificateSelectionCallback;
            this.alpnProtocols = alpnProtocols;
        }

        /// <summary>
        /// Connect to remote server
        /// </summary>
        public void Connect()
        {
            socket = new Socket(remoteIpAddress.GetAddressFamily(), SocketType.Stream, ProtocolType.Tcp);
            // try connection to the broker
            socket.Connect(remoteHostName, remotePort);

            // secure channel requested
            if (secure)
            {
                // create SSL stream
                netStream = new NetworkStream(socket);
                sslStream = new SslStream(netStream, false, userCertificateValidationCallback, userCertificateSelectionCallback);

                // server authentication (SSL/TLS handshake)
                X509CertificateCollection clientCertificates = null;
                // check if there is a client certificate to add to the collection, otherwise it's null (as empty)
                if (clientCert != null)
                    clientCertificates = new X509CertificateCollection(new X509Certificate[] { clientCert });
#if (NETSTANDARD1_6 || NETCOREAPP3_1)

                if ((alpnProtocols != null) && (0 < alpnProtocols.Count))
                {
                    sslStream = new SslStream(netStream, false);
                    SslClientAuthenticationOptions authOptions = new SslClientAuthenticationOptions();
                    List<SslApplicationProtocol> sslProtocolList = new List<SslApplicationProtocol>();
                    foreach (string alpnProtocol in alpnProtocols)
                    {
                        sslProtocolList.Add(new SslApplicationProtocol(alpnProtocol));
                    }
                    authOptions.ApplicationProtocols = sslProtocolList;
                    authOptions.EnabledSslProtocols = MqttSslUtility.ToSslPlatformEnum(sslProtocol);
                    authOptions.TargetHost = remoteHostName;
                    authOptions.AllowRenegotiation = false;
                    authOptions.ClientCertificates = clientCertificates;
                    authOptions.EncryptionPolicy = EncryptionPolicy.RequireEncryption;

                    sslStream.AuthenticateAsClientAsync(authOptions).Wait();
                }
                else
                {
                    sslStream.AuthenticateAsClientAsync(remoteHostName,
                        clientCertificates,
                        MqttSslUtility.ToSslPlatformEnum(sslProtocol),
                        false).Wait();
                }
#else
                this.sslStream.AuthenticateAsClient(this.remoteHostName,
                    clientCertificates,
                    MqttSslUtility.ToSslPlatformEnum(this.sslProtocol),
                    false);
#endif

            }
        }

        /// <summary>
        /// Send data on the network channel
        /// </summary>
        /// <param name="buffer">Data buffer to send</param>
        /// <returns>Number of byte sent</returns>
        public int Send(byte[] buffer)
        {
            if (secure)
            {
                sslStream.Write(buffer, 0, buffer.Length);
                sslStream.Flush();
                return buffer.Length;
            }
            else
                return socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        /// <summary>
        /// Receive data from the network
        /// </summary>
        /// <param name="buffer">Data buffer for receiving data</param>
        /// <returns>Number of bytes received</returns>
        public int Receive(byte[] buffer)
        {
            if (secure)
            {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < buffer.Length)
                {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = sslStream.Read(buffer, idx, buffer.Length - idx);
                    if (read == 0)
                        return 0;
                    idx += read;
                }
                return buffer.Length;
            }
            else
            {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < buffer.Length)
                {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = socket.Receive(buffer, idx, buffer.Length - idx, SocketFlags.None);
                    if (read == 0)
                        return 0;
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
        public int Receive(byte[] buffer, int timeout)
        {
            // check data availability (timeout is in microseconds)
            if (socket.Poll(timeout * 1000, SelectMode.SelectRead))
            {
                return Receive(buffer);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Close the network channel
        /// </summary>
        public void Close()
        {
            if (secure)
            {
                netStream.Flush();
                sslStream.Flush();

            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // An error occurred when attempting to access the socket or socket has been closed
                // Refer to: https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown(v=vs.110).aspx
            }
            socket.Dispose();
        }

        /// <summary>
        /// Accept connection from a remote client
        /// </summary>
        public void Accept()
        {
            // secure channel requested
            if (secure)
            {
                netStream = new NetworkStream(socket);
                sslStream = new SslStream(netStream, false, userCertificateValidationCallback, userCertificateSelectionCallback);
                sslStream.AuthenticateAsServerAsync(serverCert, false, MqttSslUtility.ToSslPlatformEnum(sslProtocol), false).Wait();
            }

            return;
        }
    }

    /// <summary>
    /// IPAddress Utility class
    /// </summary>
    public static class IPAddressUtility
    {
        /// <summary>
        /// Return AddressFamily for the IP address
        /// </summary>
        /// <param name="ipAddress">IP address to check</param>
        /// <returns>Address family</returns>
        public static AddressFamily GetAddressFamily(this IPAddress ipAddress)
        {
            return ipAddress.AddressFamily;
        }
    }

    /// <summary>
    /// MQTT SSL utility class
    /// </summary>
    public static class MqttSslUtility
    {
        public static SslProtocols ToSslPlatformEnum(MqttSslProtocols mqttSslProtocol)
        {
            switch (mqttSslProtocol)
            {
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
