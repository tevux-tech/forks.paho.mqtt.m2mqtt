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

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        public MqttClient(string brokerHostName) : this(brokerHostName, MqttSettings.BrokerDefaultPort, false, null, null, MqttSslProtocols.None) {
        }

        public MqttClient(string brokerHostName, ushort brokerPort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol) {
            Init(brokerHostName, brokerPort, secure, caCert, clientCert, sslProtocol, null, null);
        }

        public MqttClient(string brokerHostName, ushort brokerPort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback) : this(brokerHostName, brokerPort, secure, caCert, clientCert, sslProtocol, userCertificateValidationCallback, null) {
        }

        public MqttClient(string brokerHostName, ushort brokerPort, bool secure, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback) : this(brokerHostName, brokerPort, secure, null, null, sslProtocol, userCertificateValidationCallback, userCertificateSelectionCallback) {
        }

        public MqttClient(string brokerHostName, ushort brokerPort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback, List<string> ALPNProtocols = null) {
            Init(brokerHostName, brokerPort, secure, caCert, clientCert, sslProtocol, userCertificateValidationCallback, userCertificateSelectionCallback, ALPNProtocols);
        }
    }
}
