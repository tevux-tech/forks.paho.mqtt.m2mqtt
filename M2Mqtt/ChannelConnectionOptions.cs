using System;

namespace uPLibrary.Networking.M2Mqtt {
    public class ChannelConnectionOptions {
        public string Hostname { get; private set; } = "localhost";
        public ushort Port { get; private set; } = 1883;

        public void SetHostname(string hostname) {
            if (string.IsNullOrEmpty(hostname)) { throw new ArgumentException($"Argument '{nameof(hostname)}' has to be a valid non-empty string", nameof(hostname)); }

            Hostname = hostname;
        }

        public void SetPort(ushort port) {
            Port = port;
        }
    }
}
