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

using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PUBREL message from client top broker
    /// </summary>
    public class MqttMsgPubrel : MqttMsgBase {
        /// <summary>
        /// Constructor
        /// </summary>
        public MqttMsgPubrel() {
            type = MessageType.PubRel;
            // PUBREL message use QoS Level 1 (not "officially" in 3.1.1)
            qosLevel = QosLevels.AtLeastOnce;
        }

        public override byte[] GetBytes(byte protocolVersion) {
            var fixedHeaderSize = 0;
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // message identifier
            varHeaderSize += MessageIdSize;

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            var temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1) {
                buffer[index++] = (MessageType.PubRel << MSG_TYPE_OFFSET) | MessageFlags.PubRel; // [v.3.1.1]
            }
            else {
                buffer[index] = (byte)((MessageType.PubRel << MSG_TYPE_OFFSET) |
                                   (qosLevel << QOS_LEVEL_OFFSET));
                buffer[index] |= dupFlag ? (byte)(1 << DUP_FLAG_OFFSET) : (byte)0x00;
                index++;
            }

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // get next message identifier
            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            return buffer;
        }

        public static MqttMsgPubrel Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel) {
            byte[] buffer;
            var index = 0;
            var msg = new MqttMsgPubrel();

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1) {
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MessageFlags.PubRel) {
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
                }
            }

            // get remaining length and allocate buffer
            var remainingLength = DecodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            channel.Receive(buffer);

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1) {
                // only 3.1.0

                // read QoS level from fixed header (would be QoS Level 1)
                msg.qosLevel = (byte)((fixedHeaderFirstByte & QOS_LEVEL_MASK) >> QOS_LEVEL_OFFSET);
                // read DUP flag from fixed header
                msg.dupFlag = (((fixedHeaderFirstByte & DUP_FLAG_MASK) >> DUP_FLAG_OFFSET) == 0x01);
            }

            // message id
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);

            return msg;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("PUBREL", new object[] { "messageId" }, new object[] { messageId });
#else
            return base.ToString();
#endif
        }
    }
}
