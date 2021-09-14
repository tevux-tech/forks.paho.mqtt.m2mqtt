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
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for CONNECT message from client to broker
    /// </summary>
    public class MqttMsgConnect : MqttMsgBase, ISentToBroker {
        public const ushort KeepAliveDefaultValue = 60; // seconds
        public const ushort KeepAliveMaximumValue = 65535; // 16 bit

        // connect flags
        public class FlagOffset {
            public const byte Username = 0x07;
            public const byte Password = 0x06;
            public const byte WillRetain = 0x05;
            public const byte WillQoS = 0x03;
            public const byte Will = 0x02;
            public const byte CleanSession = 0x01;
        }

        public string ProtocolName { get; set; }
        public string ClientId { get; set; }

        public bool WillRetain {
            get { return willRetain; }
            set { willRetain = value; }
        }

        public byte WillQosLevel {
            get { return willQosLevel; }
            set { willQosLevel = value; }
        }

        public bool WillFlag { get; set; }
        public string WillTopic { get; set; }
        public string WillMessage { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }
        public bool CleanSession { get; set; }
        public ushort KeepAlivePeriod { get; set; }


        protected bool willRetain;
        protected byte willQosLevel;

        public MqttMsgConnect() {
            Type = MessageType.Connect;
        }

        public MqttMsgConnect(string clientId) : this(clientId, null, null, false, QosLevels.AtLeastOnce, false, null, null, true, KeepAliveDefaultValue) {
        }

        public MqttMsgConnect(string clientId, string username, string password, bool willRetain, byte willQosLevel, bool willFlag, string willTopic, string willMessage, bool cleanSession, ushort keepAlivePeriod) {
            Type = MessageType.Connect;

            ClientId = clientId;
            Username = username;
            Password = password;
            this.willRetain = willRetain;
            this.willQosLevel = willQosLevel;
            WillFlag = willFlag;
            WillTopic = willTopic;
            WillMessage = willMessage;
            CleanSession = cleanSession;
            KeepAlivePeriod = keepAlivePeriod;
        }

        public byte[] GetBytes() {
            var payloadSize = 0;
            byte[] buffer;
            var index = 0;

            var clientIdUtf8 = Encoding.UTF8.GetBytes(ClientId);
            var willTopicUtf8 = (WillFlag && (WillTopic != null)) ? Encoding.UTF8.GetBytes(WillTopic) : null;
            var willMessageUtf8 = (WillFlag && (WillMessage != null)) ? Encoding.UTF8.GetBytes(WillMessage) : null;
            var usernameUtf8 = ((Username != null) && (Username.Length > 0)) ? Encoding.UTF8.GetBytes(Username) : null;
            var passwordUtf8 = ((Password != null) && (Password.Length > 0)) ? Encoding.UTF8.GetBytes(Password) : null;


            // will flag set, will topic and will message MUST be present
            if (WillFlag && ((willQosLevel >= 0x03) || (willTopicUtf8 == null) || (willMessageUtf8 == null) || ((willTopicUtf8 != null) && (willTopicUtf8.Length == 0)) || ((willMessageUtf8 != null) && (willMessageUtf8.Length == 0)))) {
                throw new MqttClientException(MqttClientErrorCode.WillWrong);
            }
            // willflag not set, retain must be 0 and will topic and message MUST NOT be present
            else if (!WillFlag && (willRetain || (willTopicUtf8 != null) || (willMessageUtf8 != null) || ((willTopicUtf8 != null) && (willTopicUtf8.Length != 0)) || ((willMessageUtf8 != null) && (willMessageUtf8.Length != 0)))) {
                throw new MqttClientException(MqttClientErrorCode.WillWrong);
            }

            if (KeepAlivePeriod > KeepAliveMaximumValue) {
                throw new MqttClientException(MqttClientErrorCode.KeepAliveWrong);
            }

            // check on will QoS Level
            if ((willQosLevel < QosLevels.AtMostOnce) || (willQosLevel > QosLevels.ExactlyOnce)) {
                throw new MqttClientException(MqttClientErrorCode.WillWrong);
            }

            // Variable header is always 10 bytes for 3.1.1.
            var varHeaderSize = 10;

            // client identifier field size
            payloadSize += clientIdUtf8.Length + 2;
            // will topic field size
            payloadSize += (willTopicUtf8 != null) ? (willTopicUtf8.Length + 2) : 0;
            // will message field size
            payloadSize += (willMessageUtf8 != null) ? (willMessageUtf8.Length + 2) : 0;
            // username field size
            payloadSize += (usernameUtf8 != null) ? (usernameUtf8.Length + 2) : 0;
            // password field size
            payloadSize += (passwordUtf8 != null) ? (passwordUtf8.Length + 2) : 0;

            var remainingLength = varHeaderSize + payloadSize;

            var fixedHeaderSize = Helpers.CalculateFixedHeaderSize(remainingLength);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            buffer[index++] = (MessageType.Connect << FixedHeader.TypeOffset) | MessageFlags.Connect; // [v.3.1.1]

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // Protocol name is fixed by the specification. Section 3.1.2.1.
            buffer[index++] = 0;
            buffer[index++] = 4;
            buffer[index++] = (byte)'M';
            buffer[index++] = (byte)'Q';
            buffer[index++] = (byte)'T';
            buffer[index++] = (byte)'T';

            // Protocol version is 4 for 3.1.1.
            buffer[index++] = 4;

            // connect flags
            byte connectFlags = 0x00;
            connectFlags |= (usernameUtf8 != null) ? (byte)(1 << FlagOffset.Username) : (byte)0x00;
            connectFlags |= (passwordUtf8 != null) ? (byte)(1 << FlagOffset.Password) : (byte)0x00;
            connectFlags |= (willRetain) ? (byte)(1 << FlagOffset.WillRetain) : (byte)0x00;
            // only if will flag is set, we have to use will QoS level (otherwise is MUST be 0)
            if (WillFlag) {
                connectFlags |= (byte)(willQosLevel << FlagOffset.WillQoS);
            }

            connectFlags |= (WillFlag) ? (byte)(1 << FlagOffset.Will) : (byte)0x00;
            connectFlags |= (CleanSession) ? (byte)(1 << FlagOffset.CleanSession) : (byte)0x00;
            buffer[index++] = connectFlags;

            // keep alive period
            buffer[index++] = (byte)((KeepAlivePeriod >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(KeepAlivePeriod & 0x00FF); // LSB

            // client identifier
            buffer[index++] = (byte)((clientIdUtf8.Length >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(clientIdUtf8.Length & 0x00FF); // LSB
            Array.Copy(clientIdUtf8, 0, buffer, index, clientIdUtf8.Length);
            index += clientIdUtf8.Length;

            // will topic
            if (WillFlag && (willTopicUtf8 != null)) {
                buffer[index++] = (byte)((willTopicUtf8.Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(willTopicUtf8.Length & 0x00FF); // LSB
                Array.Copy(willTopicUtf8, 0, buffer, index, willTopicUtf8.Length);
                index += willTopicUtf8.Length;
            }

            // will message
            if (WillFlag && (willMessageUtf8 != null)) {
                buffer[index++] = (byte)((willMessageUtf8.Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(willMessageUtf8.Length & 0x00FF); // LSB
                Array.Copy(willMessageUtf8, 0, buffer, index, willMessageUtf8.Length);
                index += willMessageUtf8.Length;
            }

            // username
            if (usernameUtf8 != null) {
                buffer[index++] = (byte)((usernameUtf8.Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(usernameUtf8.Length & 0x00FF); // LSB
                Array.Copy(usernameUtf8, 0, buffer, index, usernameUtf8.Length);
                index += usernameUtf8.Length;
            }

            // password
            if (passwordUtf8 != null) {
                buffer[index++] = (byte)((passwordUtf8.Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(passwordUtf8.Length & 0x00FF); // LSB
                Array.Copy(passwordUtf8, 0, buffer, index, passwordUtf8.Length);
            }

            return buffer;
        }

        public override string ToString() {
            return GetTraceString("CONNECT", new object[] { "protocolName", "clientId", "willFlag", "willRetain", "willQosLevel", "willTopic", "willMessage", "username", "password", "cleanSession", "keepAlivePeriod" }, new object[] { ProtocolName, ClientId, WillFlag, willRetain, willQosLevel, WillTopic, WillMessage, Username, Password, CleanSession, KeepAlivePeriod });
        }
    }
}
