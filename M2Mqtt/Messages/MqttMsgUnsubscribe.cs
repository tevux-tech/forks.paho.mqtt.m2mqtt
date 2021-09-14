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
// if NOT .Net Micro Framework
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for UNSUBSCRIBE message from client to broker
    /// </summary>
    public class MqttMsgUnsubscribe : MqttMsgBase, ISentToBroker {

        public string[] TopicsToUnsubscribe { get; set; }

        public MqttMsgUnsubscribe() {
            type = MessageType.Unsubscribe;
        }

        public MqttMsgUnsubscribe(string[] topicsToUnsubscribe) {
            type = MessageType.Unsubscribe;

            TopicsToUnsubscribe = topicsToUnsubscribe;

            // UNSUBSCRIBE message uses QoS Level 1 (not "officially" in 3.1.1)
            qosLevel = QosLevels.AtLeastOnce;
        }

        public byte[] GetBytes() {
            var fixedHeaderSize = 0;
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // topics list empty
            if ((TopicsToUnsubscribe == null) || (TopicsToUnsubscribe.Length == 0)) {
                throw new MqttClientException(MqttClientErrorCode.TopicsEmpty);
            }

            // message identifier
            varHeaderSize += MessageIdSize;

            var topicIdx = 0;
            var topicsUtf8 = new byte[TopicsToUnsubscribe.Length][];

            for (topicIdx = 0; topicIdx < TopicsToUnsubscribe.Length; topicIdx++) {
                // check topic length
                if ((TopicsToUnsubscribe[topicIdx].Length < MinTopicLength) || (TopicsToUnsubscribe[topicIdx].Length > MaxTopicLength)) {
                    throw new MqttClientException(MqttClientErrorCode.TopicLength);
                }

                topicsUtf8[topicIdx] = Encoding.UTF8.GetBytes(TopicsToUnsubscribe[topicIdx]);
                payloadSize += 2; // topic size (MSB, LSB)
                payloadSize += topicsUtf8[topicIdx].Length;
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
            buffer[index++] = (MessageType.Unsubscribe << FixedHeader.TypeOffset) | MessageFlags.Unsubscribe; // [v.3.1.1]

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned
            if (messageId == 0) {
                throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
            }

            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            topicIdx = 0;
            for (topicIdx = 0; topicIdx < TopicsToUnsubscribe.Length; topicIdx++) {
                // topic name
                buffer[index++] = (byte)((topicsUtf8[topicIdx].Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(topicsUtf8[topicIdx].Length & 0x00FF); // LSB
                Array.Copy(topicsUtf8[topicIdx], 0, buffer, index, topicsUtf8[topicIdx].Length);
                index += topicsUtf8[topicIdx].Length;
            }

            return buffer;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("UNSUBSCRIBE", new object[] { "messageId", "topics" }, new object[] { messageId, TopicsToUnsubscribe });
#else
            return base.ToString();
#endif
        }
    }
}
