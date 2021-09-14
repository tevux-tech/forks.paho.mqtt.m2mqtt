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

using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PUBLISH message from client to broker
    /// </summary>
    public class MqttMsgPublish : MqttMsgBase, ISentToBroker {
        public string Topic { get; set; }

        public byte[] Message { get; set; }

        public MqttMsgPublish() {
            Type = MessageType.Publish;
        }

        public MqttMsgPublish(string topic, byte[] message) : this(topic, message, false, QosLevels.AtMostOnce, false) {
        }

        public MqttMsgPublish(string topic, byte[] message, bool dupFlag, byte qosLevel, bool retain) : base() {
            Type = MessageType.Publish;

            Topic = topic;
            Message = message;
            this.DupFlag = dupFlag;
            this.QosLevel = qosLevel;
            this.Retain = retain;
            MessageId = 0;
        }

        public byte[] GetBytes() {
            var fixedHeaderSize = 0;
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
            if (QosLevel > QosLevels.ExactlyOnce) {
                throw new MqttClientException(MqttClientErrorCode.QosNotAllowed);
            }

            var topicUtf8 = Encoding.UTF8.GetBytes(Topic);

            // topic name
            varHeaderSize += topicUtf8.Length + 2;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((QosLevel == QosLevels.AtLeastOnce) ||
                (QosLevel == QosLevels.ExactlyOnce)) {
                varHeaderSize += MessageIdSize;
            }

            // check on message with zero length
            if (Message != null) {
                // message data
                payloadSize += Message.Length;
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
            buffer[index] = (byte)((MessageType.Publish << FixedHeader.TypeOffset) | (QosLevel << FixedHeader.QosLevelOffset));
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
            if ((QosLevel == QosLevels.AtLeastOnce) || (QosLevel == QosLevels.ExactlyOnce)) {
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
                index += Message.Length;
            }

            return buffer;
        }

        public static MqttMsgPublish Parse(byte fixedHeaderFirstByte, IMqttNetworkChannel channel) {
            byte[] buffer;
            var index = 0;
            byte[] topicUtf8;
            int topicUtf8Length;
            var msg = new MqttMsgPublish();

            // get remaining length and allocate buffer
            var remainingLength = DecodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            var received = channel.Receive(buffer);

            // topic name
            topicUtf8Length = ((buffer[index++] << 8) & 0xFF00);
            topicUtf8Length |= buffer[index++];
            topicUtf8 = new byte[topicUtf8Length];
            Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
            index += topicUtf8Length;
            msg.Topic = new string(Encoding.UTF8.GetChars(topicUtf8));

            // read QoS level from fixed header
            msg.QosLevel = (byte)((fixedHeaderFirstByte & FixedHeader.QosLevelMask) >> FixedHeader.QosLevelOffset);
            // check wrong QoS level (both bits can't be set 1)
            if (msg.QosLevel > QosLevels.ExactlyOnce) {
                throw new MqttClientException(MqttClientErrorCode.QosNotAllowed);
            }
            // read DUP flag from fixed header
            msg.DupFlag = (((fixedHeaderFirstByte & FixedHeader.DuplicateFlagMask) >> FixedHeader.DuplicateFlagOffset) == 0x01);
            // read retain flag from fixed header
            msg.Retain = (((fixedHeaderFirstByte & FixedHeader.RetainFlagMask) >> FixedHeader.RetainFlagOffset) == 0x01);

            // message id is valid only with QOS level 1 or QOS level 2
            if ((msg.QosLevel == QosLevels.AtLeastOnce) ||
                (msg.QosLevel == QosLevels.ExactlyOnce)) {
                // message id
                msg.MessageId = (ushort)((buffer[index++] << 8) & 0xFF00);
                msg.MessageId |= (buffer[index++]);
            }

            // get payload with message data
            var messageSize = remainingLength - index;
            var remaining = messageSize;
            var messageOffset = 0;
            msg.Message = new byte[messageSize];

            // BUG FIX 26/07/2013 : receiving large payload

            // copy first part of payload data received
            Array.Copy(buffer, index, msg.Message, messageOffset, received - index);
            remaining -= (received - index);
            messageOffset += (received - index);

            // if payload isn't finished
            while (remaining > 0) {
                // receive other payload data
                received = channel.Receive(buffer);
                Array.Copy(buffer, 0, msg.Message, messageOffset, received);
                remaining -= received;
                messageOffset += received;
            }

            return msg;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("PUBLISH", new object[] { "messageId", "topic", "message" }, new object[] { MessageId, Topic, Message });
#else
            return base.ToString();
#endif
        }
    }
}
