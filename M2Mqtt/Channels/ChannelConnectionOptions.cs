/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - connection rework 2021
*/

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Tevux.Protocols.Mqtt {
    public class ChannelConnectionOptions {
        public string Hostname { get; private set; } = "localhost";
        public ushort Port { get; private set; } = 1883;
        public X509Certificate Certificate { get; private set; } = new X509Certificate();
        public SslProtocols MinimumSslProtocol { get; private set; } = SslProtocols.None;
        public bool IsTlsUsed { get; private set; }
        public bool IsReconnectionEnabled { get; private set; } = true;
        public RemoteCertificateValidationCallback UserCertificateValidationCallback { get; private set; }
        public LocalCertificateSelectionCallback UserCertificateSelectionCallback { get; private set; }


        public ChannelConnectionOptions() {
            UserCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                // Accepting all certificates. Will return to this when there's an actual need.
                return true;
            };
            UserCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => {
                // Just picking first certificate in the list. Will return to this when there's an actual need.
                var returnCertificate = new X509Certificate();

                if (localCertificates.Count > 0) {
                    returnCertificate = localCertificates[0];
                }

                return returnCertificate;
            };
        }

        public override string ToString() {
            return $"{Hostname}:{Port}";
        }

        public void SetHostname(string hostname) {
            if (string.IsNullOrEmpty(hostname)) { throw new ArgumentException($"Argument '{nameof(hostname)}' has to be a valid non-empty string", nameof(hostname)); }

            Hostname = hostname;
        }

        public void SetPort(ushort port) {
            Port = port;
        }

        public void SetCertificate(X509Certificate certificate) {
            Certificate = certificate;
            IsTlsUsed = true;
        }

        public void SetReconnection(bool isReconnectionEnabled) {
            IsReconnectionEnabled = isReconnectionEnabled;
        }
    }
}
