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
using System.Text;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for CONNECT message from client to broker. See section 3.1.
    /// </summary>
    internal class MqttMsgConnect : MqttMsgBase {
#warning should get that from connection options
        public const ushort KeepAliveDefaultValue = 15; // seconds

        // connect flags
        public class FlagOffset {
            public const byte Username = 0x07;
            public const byte Password = 0x06;
            public const byte WillRetain = 0x05;
            public const byte WillQoS = 0x03;
            public const byte Will = 0x02;
            public const byte CleanSession = 0x01;
        }

        public MqttConnectionOptions ConnectionOptions { get; private set; }

        public MqttMsgConnect() {
            Type = MessageType.Connect;
        }

        public MqttMsgConnect(MqttConnectionOptions connectionOptions) : this() {
            ConnectionOptions = connectionOptions;
        }

        public override byte[] GetBytes() {
            // This is the most complicated packet of them all, with multiple sections:
            // 1. Fixed header (mandatory, variable length)
            // 2. Variable header (mandatory, fixed length)
            // 3. Client id section (mandatory, variable length)
            // 4. Will section (optional, variable length)
            // 5. Credentials section (optional, variable length)

            var flags = (ConnectionOptions.IsCleanSession ? 1 : 0) << 1;

            // First we need to calculate length of all variable sections. Let's go with client ID section first, as it must be present.
            // Specification allows for empty content, but also discourages it.
            var clientIdBytes = Encoding.UTF8.GetBytes(ConnectionOptions.ClientId);
            var clientIdPayload = new byte[2 + clientIdBytes.Length];
            clientIdPayload[0] = (byte)(clientIdBytes.Length >> 8);
            clientIdPayload[1] = (byte)(clientIdBytes.Length & 0xFF);
            Array.Copy(clientIdBytes, 0, clientIdPayload, 2, clientIdBytes.Length);

            // Now, the optional Will section.
            var willPayload = new byte[0];
            if (ConnectionOptions.IsWillUsed) {
                var willPayloadSize = 0;

                // Technically, specification allows for zero-length topics. Wouldn't make much sense, though...
                willPayloadSize += ConnectionOptions.WillTopic.Length + 2;

                // Will message length can be zero.
                willPayloadSize += ConnectionOptions.WillMessage.Length + 2;

                // Building the payload. 
                willPayload = new byte[willPayloadSize];

                var willTopicBytes = Encoding.UTF8.GetBytes(ConnectionOptions.WillTopic);
                willPayload[0] = (byte)(willTopicBytes.Length >> 8);
                willPayload[1] = (byte)(willTopicBytes.Length & 0xFF);
                Array.Copy(willTopicBytes, 0, willPayload, 2, willTopicBytes.Length);

                willPayload[willPayload.Length + 2 + 0] = (byte)(ConnectionOptions.WillMessage.Length >> 8);
                willPayload[willPayload.Length + 2 + 1] = (byte)(ConnectionOptions.WillMessage.Length & 0xFF);
                Array.Copy(ConnectionOptions.WillMessage, 0, willPayload, willTopicBytes.Length + 2 + 2, ConnectionOptions.WillMessage.Length);

                // Setting appropriate flag bits.
                flags |= 1 << 2;
                flags |= (byte)ConnectionOptions.WillQosLevel << 3;
                flags |= (ConnectionOptions.IsWillRetained ? 1 : 0) << 5;
            }

            // Final variable section - credentials.
            var credentialsPayload = new byte[0];
            if (string.IsNullOrEmpty(ConnectionOptions.Username) == false) {
                var credentialsPayloadSize = 0;

                // Technically, MQTT 3.1.1 specification does not tell if username string can be empty or not.
                // But empty username would be kind of stupid, so I'll just assume it has at least a single symbol.
                credentialsPayloadSize += ConnectionOptions.Username.Length + 2;

                // However, password is allowed to be empty. See section 3.1.3.5.
                // But even if it is empty, payload bytes are still present.
                credentialsPayloadSize += ConnectionOptions.Password.Length + 2;

                // Building the payload. Need to UTF-8 encode strings.
                credentialsPayload = new byte[credentialsPayloadSize];

                var usernameBytes = Encoding.UTF8.GetBytes(ConnectionOptions.Username);
                credentialsPayload[0] = (byte)(usernameBytes.Length >> 8);
                credentialsPayload[1] = (byte)(usernameBytes.Length & 0xFF);
                Array.Copy(usernameBytes, 0, credentialsPayload, 2, usernameBytes.Length);

                var passwordBytes = Encoding.UTF8.GetBytes(ConnectionOptions.Password);
                credentialsPayload[usernameBytes.Length + 2 + 0] = (byte)(passwordBytes.Length >> 8);
                credentialsPayload[usernameBytes.Length + 2 + 1] = (byte)(passwordBytes.Length & 0xFF);
                Array.Copy(passwordBytes, 0, credentialsPayload, usernameBytes.Length + 2 + 2, passwordBytes.Length);

                // Setting appropriate flag bits. Here, MQTT spec complicated things a bit, as it allows username to be present, but password missing.
                // I will cheat here a bit, and will set password flag anyway, hoping that nobody really uses username only,
                // password being not just empty, but completely missing altogether. If somebody complains, I'll fix it then...
                flags |= 1 << 7;
                flags |= 1 << 6;
            }

            // Variable header is always 10 bytes for 3.1.1.
            var varHeaderPayload = new byte[10];
            varHeaderPayload[0] = 0;
            varHeaderPayload[1] = 4;
            varHeaderPayload[2] = (byte)'M';
            varHeaderPayload[3] = (byte)'Q';
            varHeaderPayload[4] = (byte)'T';
            varHeaderPayload[5] = (byte)'T';
            varHeaderPayload[6] = 4; // <-- Protocol ID. 
            varHeaderPayload[7] = (byte)flags;
            varHeaderPayload[8] = (byte)(ConnectionOptions.KeepAlivePeriod >> 8);
            varHeaderPayload[9] = (byte)(ConnectionOptions.KeepAlivePeriod & 0xFF);

            // Now we have all the size, so we can calculate fixed header size.
            var remainingLength = varHeaderPayload.Length + clientIdPayload.Length + credentialsPayload.Length + willPayload.Length;
            var fixedHeaderSize = CalculateFixedHeaderSize(remainingLength);

            // Finally, building the resulting full payload.
            var finalBuffer = new byte[fixedHeaderSize + remainingLength];
            finalBuffer[0] = (byte)(Type << 4);
            EncodeRemainingLength(remainingLength, finalBuffer, 1);
            Array.Copy(varHeaderPayload, 0, finalBuffer, fixedHeaderSize, varHeaderPayload.Length);
            Array.Copy(clientIdPayload, 0, finalBuffer, fixedHeaderSize + varHeaderPayload.Length, clientIdPayload.Length);
            Array.Copy(willPayload, 0, finalBuffer, fixedHeaderSize + varHeaderPayload.Length + clientIdPayload.Length, willPayload.Length);
            Array.Copy(credentialsPayload, 0, finalBuffer, fixedHeaderSize + varHeaderPayload.Length + clientIdPayload.Length + willPayload.Length, credentialsPayload.Length);

            return finalBuffer;
        }

        public override string ToString() {
            return GetTraceString("CONNECT", new object[] { "clientId", "willFlag", "willRetain", "willQosLevel", "willTopic", "willMessage", "username", "password", "cleanSession", "keepAlivePeriod" }, new object[] { ConnectionOptions.ClientId, ConnectionOptions.IsWillUsed, ConnectionOptions.IsWillRetained, ConnectionOptions.WillQosLevel, ConnectionOptions.WillTopic, ConnectionOptions.WillMessage, ConnectionOptions.Username, ConnectionOptions.Password, ConnectionOptions.IsCleanSession, ConnectionOptions.KeepAlivePeriod });
        }
    }
}
