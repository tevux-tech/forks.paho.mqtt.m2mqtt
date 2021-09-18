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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PUBLISH message from client to broker. See section 3.3.
    /// </summary>
    public class MqttMsgPublish : MqttMsgBase, ISentToBroker {
        public string Topic { get; set; }

        public byte[] Message { get; set; }

        internal MqttMsgPublish() {
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
            // Variable header section.
            var topicBytes = Encoding.UTF8.GetBytes(Topic);
            var variableHeaderSize = topicBytes.Length + 2;

            var messageIdBytes = new byte[0];
            if ((QosLevel == QosLevel.AtLeastOnce) || (QosLevel == QosLevel.ExactlyOnce)) {
                // For QoS levels 1 and 2, a Packet ID has to be appended to variable header.
                variableHeaderSize += 2;
                messageIdBytes = new byte[2];
                messageIdBytes[0] = (byte)(MessageId >> 8);
                messageIdBytes[1] = (byte)(MessageId & 0xFF);
            }

            var variableHeaderBytes = new byte[variableHeaderSize];
            variableHeaderBytes[0] = (byte)(topicBytes.Length >> 8);
            variableHeaderBytes[1] = (byte)(topicBytes.Length & 0xFF);
            Array.Copy(topicBytes, 0, variableHeaderBytes, 2, topicBytes.Length);
            Array.Copy(messageIdBytes, 0, variableHeaderBytes, 2 + topicBytes.Length, messageIdBytes.Length);

            // Now we have all the sizes, so we can calculate fixed header size.
            var remainingLength = variableHeaderBytes.Length + Message.Length;
            var fixedHeaderSize = Helpers.CalculateFixedHeaderSize(remainingLength);

            // Finally, building the resulting full payload.
            var finalBuffer = new byte[fixedHeaderSize + remainingLength];
            finalBuffer[0] = MessageType.Publish << 4;
            finalBuffer[0] += (byte)((DupFlag ? 1 : 0) << 3);
            finalBuffer[0] += (byte)(((byte)QosLevel) << 1);
            finalBuffer[0] += (byte)((Retain ? 1 : 0) << 0);
            EncodeRemainingLength(remainingLength, finalBuffer, 1);
            Array.Copy(variableHeaderBytes, 0, finalBuffer, fixedHeaderSize, variableHeaderBytes.Length);
            Array.Copy(Message, 0, finalBuffer, fixedHeaderSize + variableHeaderBytes.Length, Message.Length);

            return finalBuffer;
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
