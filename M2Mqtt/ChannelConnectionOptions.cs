using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Tevux.Protocols.Mqtt {
    public class ChannelConnectionOptions {
        public string Hostname { get; private set; } = "localhost";
        public ushort Port { get; private set; } = 1883;
        public X509Certificate ClientCertificate { get; private set; } = new X509Certificate();
        public MqttSslProtocols SslProtocol { get; private set; } = MqttSslProtocols.None;
        public RemoteCertificateValidationCallback UserCertificateValidationCallback { get; private set; }
        public LocalCertificateSelectionCallback UserCertificateSelectionCallback { get; private set; }

        public ChannelConnectionOptions() {
            UserCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return false; };
            UserCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => { return new X509Certificate(); };
        }

        public void SetHostname(string hostname) {
            if (string.IsNullOrEmpty(hostname)) { throw new ArgumentException($"Argument '{nameof(hostname)}' has to be a valid non-empty string", nameof(hostname)); }

            Hostname = hostname;
        }

        public void SetPort(ushort port) {
            Port = port;
        }

        public void SetCertificates(X509Certificate clientCertificate, MqttSslProtocols sslProtocol, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback) {
            ClientCertificate = clientCertificate;
            SslProtocol = sslProtocol;
            UserCertificateValidationCallback = userCertificateValidationCallback;
            UserCertificateSelectionCallback = userCertificateSelectionCallback;
        }
    }
}
