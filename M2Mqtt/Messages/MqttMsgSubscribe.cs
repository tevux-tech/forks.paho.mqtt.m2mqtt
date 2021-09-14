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
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for SUBSCRIBE message from client to broker
    /// </summary>
    public class MqttMsgSubscribe : MqttMsgBase {

        public string[] Topics { get; set; }

        public byte[] QoSLevels { get; set; }

        public MqttMsgSubscribe() {
            type = MessageType.Subscribe;
        }

        public MqttMsgSubscribe(string[] topics, byte[] qosLevels) {
            type = MessageType.Subscribe;

            Topics = topics;
            QoSLevels = qosLevels;

            // SUBSCRIBE message uses QoS Level 1 (not "officially" in 3.1.1)
            qosLevel = QosLevels.AtLeastOnce;
        }

        public static MqttMsgSubscribe Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel) {
            byte[] buffer;
            var index = 0;
            byte[] topicUtf8;
            int topicUtf8Length;
            var msg = new MqttMsgSubscribe();

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1) {
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & FixedHeader.FlagBitsMask) != MessageFlags.Subcribe) {
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
                }
            }

            // get remaining length and allocate buffer
            var remainingLength = DecodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            var received = channel.Receive(buffer);

            // message id
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);

            // payload contains topics and QoS levels
            // NOTE : before, I don't know how many topics will be in the payload (so use List)

            IList<string> tmpTopics = new List<string>();
            IList<byte> tmpQosLevels = new List<byte>();

            do {
                // topic name
                topicUtf8Length = ((buffer[index++] << 8) & 0xFF00);
                topicUtf8Length |= buffer[index++];
                topicUtf8 = new byte[topicUtf8Length];
                Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
                index += topicUtf8Length;
                tmpTopics.Add(new string(Encoding.UTF8.GetChars(topicUtf8)));

                // QoS level
                tmpQosLevels.Add(buffer[index++]);

            } while (index < remainingLength);

            // copy from list to array
            msg.Topics = new string[tmpTopics.Count];
            msg.QoSLevels = new byte[tmpQosLevels.Count];
            for (var i = 0; i < tmpTopics.Count; i++) {
                msg.Topics[i] = (string)tmpTopics[i];
                msg.QoSLevels[i] = (byte)tmpQosLevels[i];
            }

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion) {
            var fixedHeaderSize = 0;
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // topics list empty
            if ((Topics == null) || (Topics.Length == 0)) {
                throw new MqttClientException(MqttClientErrorCode.TopicsEmpty);
            }

            // qos levels list empty
            if ((QoSLevels == null) || (QoSLevels.Length == 0)) {
                throw new MqttClientException(MqttClientErrorCode.QosLevelsEmpty);
            }

            // topics and qos levels lists length don't match
            if (Topics.Length != QoSLevels.Length) {
                throw new MqttClientException(MqttClientErrorCode.TopicsQosLevelsNotMatch);
            }

            // message identifier
            varHeaderSize += MessageIdSize;

            var topicIdx = 0;
            var topicsUtf8 = new byte[Topics.Length][];

            for (topicIdx = 0; topicIdx < Topics.Length; topicIdx++) {
                // check topic length
                if ((Topics[topicIdx].Length < MinTopicLength) || (Topics[topicIdx].Length > MaxTopicLength)) {
                    throw new MqttClientException(MqttClientErrorCode.TopicLength);
                }

                topicsUtf8[topicIdx] = Encoding.UTF8.GetBytes(Topics[topicIdx]);
                payloadSize += 2; // topic size (MSB, LSB)
                payloadSize += topicsUtf8[topicIdx].Length;
                payloadSize++; // byte for QoS
            }

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
                buffer[index++] = (MessageType.Subscribe << FixedHeader.TypeOffset) | MessageFlags.Subcribe; // [v.3.1.1]
            }
            else {
                buffer[index] = (byte)((MessageType.Subscribe << FixedHeader.TypeOffset) |
                                   (qosLevel << FixedHeader.QosLevelOffset));
                buffer[index] |= dupFlag ? (byte)(1 << FixedHeader.DuplicateFlagOffset) : (byte)0x00;
                index++;
            }

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
            if (messageId == 0) {
                throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
            }

            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            topicIdx = 0;
            for (topicIdx = 0; topicIdx < Topics.Length; topicIdx++) {
                // topic name
                buffer[index++] = (byte)((topicsUtf8[topicIdx].Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(topicsUtf8[topicIdx].Length & 0x00FF); // LSB
                Array.Copy(topicsUtf8[topicIdx], 0, buffer, index, topicsUtf8[topicIdx].Length);
                index += topicsUtf8[topicIdx].Length;

                // requested QoS
                buffer[index++] = QoSLevels[topicIdx];
            }

            return buffer;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("SUBSCRIBE", new object[] { "messageId", "topics", "qosLevels" }, new object[] { messageId, Topics, QoSLevels });
#else
            return base.ToString();
#endif
        }
    }
}
