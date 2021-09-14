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
    /// Class for SUBSCRIBE message from client to broker
    /// </summary>
    public class MqttMsgSubscribe : MqttMsgBase, ISentToBroker {

        public string[] Topics { get; set; }

        public byte[] QoSLevels { get; set; }

        public MqttMsgSubscribe() {
            Type = MessageType.Subscribe;
        }

        public MqttMsgSubscribe(string[] topics, byte[] qosLevels) {
            Type = MessageType.Subscribe;

            Topics = topics;
            QoSLevels = qosLevels;

            // SUBSCRIBE message uses QoS Level 1 (not "officially" in 3.1.1)
            QosLevel = QosLevels.AtLeastOnce;
        }

        public byte[] GetBytes() {
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
            var topicsUtf8 = new byte[Topics.Length][];


            int topicIdx;
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

            var fixedHeaderSize = Helpers.CalculateFixedHeaderSize(remainingLength);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            buffer[index++] = (MessageType.Subscribe << FixedHeader.TypeOffset) | MessageFlags.Subcribe; // [v.3.1.1]


            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
            if (MessageId == 0) {
                throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
            }

            buffer[index++] = (byte)((MessageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(MessageId & 0x00FF); // LSB 

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
            return GetTraceString("SUBSCRIBE", new object[] { "messageId", "topics", "qosLevels" }, new object[] { MessageId, Topics, QoSLevels });
        }
    }
}
