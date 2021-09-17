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
    /// Class for PUBLISH message from client to broker. See section 3.3.
    /// </summary>
    public class MqttMsgPublish : MqttMsgBase, ISentToBroker {
        public string Topic { get; set; }

        public byte[] Message { get; set; }

        public MqttMsgPublish() {
            Type = MessageType.Publish;
        }

        public MqttMsgPublish(string topic, byte[] message) : this(topic, message, false, QosLevel.AtMostOnce, false) {
        }

        public MqttMsgPublish(string topic, byte[] message, bool dupFlag, QosLevel qosLevel, bool retain) : base() {
            Type = MessageType.Publish;

            Topic = topic;
            Message = message;
            DupFlag = dupFlag;
            QosLevel = qosLevel;
            Retain = retain;
            MessageId = 0;
        }

        public byte[] GetBytes() {
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // topic can't contain wildcards
            if ((Topic.IndexOf('#') != -1) || (Topic.IndexOf('+') != -1)) {
                throw new MqttClientException(MqttClientErrorCode.TopicWildcard);
            }

            // check topic length
            if ((Topic.Length < MinTopicLength) || (Topic.Length > MaxTopicLength)) {
                throw new MqttClientException(MqttClientErrorCode.TopicLength);
            }

            // check wrong QoS level (both bits can't be set 1)
            if (QosLevel > QosLevel.ExactlyOnce) {
                throw new MqttClientException(MqttClientErrorCode.QosNotAllowed);
            }

            var topicUtf8 = Encoding.UTF8.GetBytes(Topic);

            // topic name
            varHeaderSize += topicUtf8.Length + 2;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((QosLevel == QosLevel.AtLeastOnce) || (QosLevel == QosLevel.ExactlyOnce)) {
                varHeaderSize += MessageIdSize;
            }

            // check on message with zero length
            if (Message != null) {
                // message data
                payloadSize += Message.Length;
            }

            remainingLength += (varHeaderSize + payloadSize);

            var fixedHeaderSize = Helpers.CalculateFixedHeaderSize(remainingLength);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            buffer[index] = (byte)((MessageType.Publish << FixedHeader.TypeOffset) | (((byte)QosLevel) << FixedHeader.QosLevelOffset));
            buffer[index] |= DupFlag ? (byte)(1 << FixedHeader.DuplicateFlagOffset) : (byte)0x00;
            buffer[index] |= Retain ? (byte)(1 << FixedHeader.RetainFlagOffset) : (byte)0x00;
            index++;

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // topic name
            buffer[index++] = (byte)((topicUtf8.Length >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(topicUtf8.Length & 0x00FF); // LSB
            Array.Copy(topicUtf8, 0, buffer, index, topicUtf8.Length);
            index += topicUtf8.Length;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((QosLevel == QosLevel.AtLeastOnce) || (QosLevel == QosLevel.ExactlyOnce)) {
                // check message identifier assigned
                if (MessageId == 0) {
                    throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
                }

                buffer[index++] = (byte)((MessageId >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(MessageId & 0x00FF); // LSB
            }

            // check on message with zero length
            if (Message != null) {
                // message data
                Array.Copy(Message, 0, buffer, index, Message.Length);
            }

            return buffer;
        }

        public static bool TryParse(byte flags, byte[] variableHeaderAndPayloadBytes, out MqttMsgPublish parsedMessage) {
            var isOk = true;
            parsedMessage = new MqttMsgPublish();

            // topic name
            var topicUtf8Length = (variableHeaderAndPayloadBytes[0] << 8) + variableHeaderAndPayloadBytes[1];


            //topicUtf8 = new byte[topicUtf8Length];
            //Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
            //index += topicUtf8Length;

            parsedMessage.Topic = new string(Encoding.UTF8.GetChars(variableHeaderAndPayloadBytes, 2, topicUtf8Length));

            // read QoS level from fixed header
            parsedMessage.QosLevel = (QosLevel)((flags & 0b0110) >> 1);
            // check wrong QoS level (both bits can't be set 1)
            if (parsedMessage.QosLevel > QosLevel.ExactlyOnce) {
                isOk = false;
            }

            // read DUP flag from fixed header
            parsedMessage.DupFlag = (flags >> 3) == 0x01;

            // read retain flag from fixed header
            parsedMessage.Retain = ((flags & 0x01) >> FixedHeader.RetainFlagOffset) == 0x01;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((parsedMessage.QosLevel == QosLevel.AtLeastOnce) || (parsedMessage.QosLevel == QosLevel.ExactlyOnce)) {
                // message id
                parsedMessage.MessageId = (ushort)((variableHeaderAndPayloadBytes[topicUtf8Length] << 8) + variableHeaderAndPayloadBytes[topicUtf8Length + 1]);
            }

            // get payload with message data
            var payloadSize = variableHeaderAndPayloadBytes.Length - topicUtf8Length - 2;
            if ((parsedMessage.QosLevel == QosLevel.AtLeastOnce) || (parsedMessage.QosLevel == QosLevel.ExactlyOnce)) {
                payloadSize -= 2;
            }

            var payloadOffset = variableHeaderAndPayloadBytes.Length - payloadSize;
            parsedMessage.Message = new byte[payloadSize];

            Array.Copy(variableHeaderAndPayloadBytes, payloadOffset, parsedMessage.Message, 0, payloadSize);

            return isOk;
        }

        public override string ToString() {
            return Helpers.GetTraceString("PUBLISH", new object[] { "messageId", "topic", "message" }, new object[] { MessageId, Topic, Message });
        }
    }
}
