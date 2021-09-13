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

namespace uPLibrary.Networking.M2Mqtt.Messages
{
    /// <summary>
    /// Class for PUBLISH message from client to broker
    /// </summary>
    public class MqttMsgPublish : MqttMsgBase
    {
        public string Topic { get; set; }

        public byte[] Message { get; set; }

        public MqttMsgPublish()
        {
            type = MessageType.Publish;
        }

        public MqttMsgPublish(string topic, byte[] message) : this(topic, message, false, QosLevels.AtMostOnce, false)
        {
        }

        public MqttMsgPublish(string topic, byte[] message, bool dupFlag, byte qosLevel, bool retain) : base()
        {
            type = MessageType.Publish;

            Topic = topic;
            Message = message;
            this.dupFlag = dupFlag;
            this.qosLevel = qosLevel;
            this.retain = retain;
            messageId = 0;
        }

        public override byte[] GetBytes(byte protocolVersion)
        {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] buffer;
            int index = 0;

            // topic can't contain wildcards
            if ((Topic.IndexOf('#') != -1) || (Topic.IndexOf('+') != -1))
                throw new MqttClientException(MqttClientErrorCode.TopicWildcard);

            // check topic length
            if ((Topic.Length < MinTopicLength) || (Topic.Length > MaxTopicLength))
                throw new MqttClientException(MqttClientErrorCode.TopicLength);

            // check wrong QoS level (both bits can't be set 1)
            if (qosLevel > QosLevels.ExactlyOnce)
                throw new MqttClientException(MqttClientErrorCode.QosNotAllowed);

            byte[] topicUtf8 = Encoding.UTF8.GetBytes(Topic);

            // topic name
            varHeaderSize += topicUtf8.Length + 2;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((qosLevel == QosLevels.AtLeastOnce) ||
                (qosLevel == QosLevels.ExactlyOnce))
            {
                varHeaderSize += MessageIdSize;
            }

            // check on message with zero length
            if (Message != null)
                // message data
                payloadSize += Message.Length;

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
            buffer[index] = (byte)((MessageType.Publish << MSG_TYPE_OFFSET) |
                                   (qosLevel << QOS_LEVEL_OFFSET));
            buffer[index] |= dupFlag ? (byte)(1 << DUP_FLAG_OFFSET) : (byte)0x00;
            buffer[index] |= retain ? (byte)(1 << RETAIN_FLAG_OFFSET) : (byte)0x00;
            index++;

            // encode remaining length
            index = encodeRemainingLength(remainingLength, buffer, index);

            // topic name
            buffer[index++] = (byte)((topicUtf8.Length >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(topicUtf8.Length & 0x00FF); // LSB
            Array.Copy(topicUtf8, 0, buffer, index, topicUtf8.Length);
            index += topicUtf8.Length;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((qosLevel == QosLevels.AtLeastOnce) ||
                (qosLevel == QosLevels.ExactlyOnce))
            {
                // check message identifier assigned
                if (messageId == 0)
                    throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
                buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(messageId & 0x00FF); // LSB
            }

            // check on message with zero length
            if (Message != null)
            {
                // message data
                Array.Copy(Message, 0, buffer, index, Message.Length);
                index += Message.Length;
            }

            return buffer;
        }

        /// <summary>
        /// Parse bytes for a PUBLISH message
        /// </summary>
        /// <param name="fixedHeaderFirstByte">First fixed header byte</param>
        /// <param name="protocolVersion">Protocol Version</param>
        /// <param name="channel">Channel connected to the broker</param>
        /// <returns>PUBLISH message instance</returns>
        public static MqttMsgPublish Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel)
        {
            byte[] buffer;
            int index = 0;
            byte[] topicUtf8;
            int topicUtf8Length;
            MqttMsgPublish msg = new MqttMsgPublish();

            // get remaining length and allocate buffer
            int remainingLength = decodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            int received = channel.Receive(buffer);

            // topic name
            topicUtf8Length = ((buffer[index++] << 8) & 0xFF00);
            topicUtf8Length |= buffer[index++];
            topicUtf8 = new byte[topicUtf8Length];
            Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
            index += topicUtf8Length;
            msg.Topic = new string(Encoding.UTF8.GetChars(topicUtf8));

            // read QoS level from fixed header
            msg.qosLevel = (byte)((fixedHeaderFirstByte & QOS_LEVEL_MASK) >> QOS_LEVEL_OFFSET);
            // check wrong QoS level (both bits can't be set 1)
            if (msg.qosLevel > QosLevels.ExactlyOnce)
                throw new MqttClientException(MqttClientErrorCode.QosNotAllowed);
            // read DUP flag from fixed header
            msg.dupFlag = (((fixedHeaderFirstByte & DUP_FLAG_MASK) >> DUP_FLAG_OFFSET) == 0x01);
            // read retain flag from fixed header
            msg.retain = (((fixedHeaderFirstByte & RETAIN_FLAG_MASK) >> RETAIN_FLAG_OFFSET) == 0x01);

            // message id is valid only with QOS level 1 or QOS level 2
            if ((msg.qosLevel == QosLevels.AtLeastOnce) ||
                (msg.qosLevel == QosLevels.ExactlyOnce))
            {
                // message id
                msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
                msg.messageId |= (buffer[index++]);
            }

            // get payload with message data
            int messageSize = remainingLength - index;
            int remaining = messageSize;
            int messageOffset = 0;
            msg.Message = new byte[messageSize];

            // BUG FIX 26/07/2013 : receiving large payload

            // copy first part of payload data received
            Array.Copy(buffer, index, msg.Message, messageOffset, received - index);
            remaining -= (received - index);
            messageOffset += (received - index);

            // if payload isn't finished
            while (remaining > 0)
            {
                // receive other payload data
                received = channel.Receive(buffer);
                Array.Copy(buffer, 0, msg.Message, messageOffset, received);
                remaining -= received;
                messageOffset += received;
            }

            return msg;
        }

        public override string ToString()
        {
#if TRACE
            return GetTraceString(
                "PUBLISH",
                new object[] { "messageId", "topic", "message" },
                new object[] { messageId, Topic, Message });
#else
            return base.ToString();
#endif
        }
    }
}
