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
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages
{
    /// <summary>
    /// Class for CONNACK message from broker to client
    /// </summary>
    public class MqttMsgConnack : MqttMsgBase
    {
        #region Constants...

        // return codes for CONNACK message
        public const byte CONN_ACCEPTED = 0x00;
        public const byte CONN_REFUSED_PROT_VERS = 0x01;
        public const byte CONN_REFUSED_IDENT_REJECTED = 0x02;
        public const byte CONN_REFUSED_SERVER_UNAVAILABLE = 0x03;
        public const byte CONN_REFUSED_USERNAME_PASSWORD = 0x04;
        public const byte CONN_REFUSED_NOT_AUTHORIZED = 0x05;

        private const byte TOPIC_NAME_COMP_RESP_BYTE_OFFSET = 0;
        private const byte TOPIC_NAME_COMP_RESP_BYTE_SIZE = 1;
        // [v3.1.1] connect acknowledge flags replace "old" topic name compression respone (not used in 3.1)
        private const byte CONN_ACK_FLAGS_BYTE_OFFSET = 0;
        private const byte CONN_ACK_FLAGS_BYTE_SIZE = 1;
        // [v3.1.1] session present flag
        private const byte SESSION_PRESENT_FLAG_MASK = 0x01;
        private const byte SESSION_PRESENT_FLAG_OFFSET = 0x00;
        private const byte SESSION_PRESENT_FLAG_SIZE = 0x01;
        private const byte CONN_RETURN_CODE_BYTE_OFFSET = 1;
        private const byte CONN_RETURN_CODE_BYTE_SIZE = 1;

        #endregion


        // [v3.1.1] session present flag
        /// <summary>
        /// Session present flag
        /// </summary>
        public bool SessionPresent { get; set; }

        public byte ReturnCode { get; set; }

        public MqttMsgConnack()
        {
            type = MQTT_MSG_CONNACK_TYPE;
        }

        public static MqttMsgConnack Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel)
        {
            byte[] buffer;
            MqttMsgConnack msg = new MqttMsgConnack();

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
            {
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MQTT_MSG_CONNACK_FLAG_BITS)
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
            }

            // get remaining length and allocate buffer
            int remainingLength = decodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            channel.Receive(buffer);
            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
            {
                // [v3.1.1] ... set session present flag ...
                msg.SessionPresent = (buffer[CONN_ACK_FLAGS_BYTE_OFFSET] & SESSION_PRESENT_FLAG_MASK) != 0x00;
            }
            // ...and set return code from broker
            msg.ReturnCode = buffer[CONN_RETURN_CODE_BYTE_OFFSET];

            return msg;
        }

        public override byte[] GetBytes(byte ProtocolVersion)
        {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] buffer;
            int index = 0;

            if (ProtocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                // flags byte and connect return code
                varHeaderSize += (CONN_ACK_FLAGS_BYTE_SIZE + CONN_RETURN_CODE_BYTE_SIZE);
            else
                // topic name compression response and connect return code
                varHeaderSize += (TOPIC_NAME_COMP_RESP_BYTE_SIZE + CONN_RETURN_CODE_BYTE_SIZE);

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do
            {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            if (ProtocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                buffer[index++] = (MQTT_MSG_CONNACK_TYPE << MSG_TYPE_OFFSET) | MQTT_MSG_CONNACK_FLAG_BITS; // [v.3.1.1]
            else
                buffer[index++] = (byte)(MQTT_MSG_CONNACK_TYPE << MSG_TYPE_OFFSET);

            // encode remaining length
            index = encodeRemainingLength(remainingLength, buffer, index);

            if (ProtocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                // [v3.1.1] session present flag
                buffer[index++] = SessionPresent ? (byte)(1 << SESSION_PRESENT_FLAG_OFFSET) : (byte)0x00;
            else
                // topic name compression response (reserved values. not used);
                buffer[index++] = 0x00;

            // connect return code
            buffer[index++] = ReturnCode;

            return buffer;
        }

        public override string ToString()
        {
#if TRACE
            return GetTraceString(
                "CONNACK",
                new object[] { "returnCode" },
                new object[] { ReturnCode });
#else
            return base.ToString();
#endif
        }
    }
}
