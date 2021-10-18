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
   Simonas Greicius - 2021 rework and simplification
*/

using System;
using System.Text;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for PUBLISH packet from client to broker. See section 3.3.
    /// </summary>
    internal class PublishPacket : ControlPacketBase {
        public string Topic { get; private set; }
        public byte[] Message { get; private set; }
        public QosLevel QosLevel { get; private set; }
        public bool DuplicateFlag { get; internal set; }
        public bool RetainFlag { get; private set; }

        internal PublishPacket() {
            Type = PacketTypes.Publish;
        }

        public PublishPacket(string topic, byte[] message, QosLevel qosLevel, bool retain) : this() {
            if ((topic.IndexOf('#') != -1) || (topic.IndexOf('+') != -1)) { throw new ArgumentException("Topic cannot contain wildcards in Publish message"); }
            if (Encoding.UTF8.GetByteCount(topic) > 65535) { throw new ArgumentException("Topic is too long. Maximum length is 65535."); }

            PacketId = GetNewPacketId();
            Topic = topic;
            Message = message;
            // DupFlag will be set in the corresponding state machine, when retransmission occurs. This is why it has internal access.
            // DupFlag = dupFlag;
            QosLevel = qosLevel;
            RetainFlag = retain;
        }

        public override byte[] GetBytes() {
            // Variable header section.
            var topicBytes = Encoding.UTF8.GetBytes(Topic);
            var variableHeaderSize = topicBytes.Length + 2;

            var packetIdBytes = new byte[0];
            if ((QosLevel == QosLevel.AtLeastOnce) || (QosLevel == QosLevel.ExactlyOnce)) {
                // For QoS levels 1 and 2, a Packet ID has to be appended to variable header.
                variableHeaderSize += 2;
                packetIdBytes = new byte[2];
                packetIdBytes[0] = (byte)(PacketId >> 8);
                packetIdBytes[1] = (byte)(PacketId & 0xFF);
            }

            var variableHeaderBytes = new byte[variableHeaderSize];
            variableHeaderBytes[0] = (byte)(topicBytes.Length >> 8);
            variableHeaderBytes[1] = (byte)(topicBytes.Length & 0xFF);
            Array.Copy(topicBytes, 0, variableHeaderBytes, 2, topicBytes.Length);
            Array.Copy(packetIdBytes, 0, variableHeaderBytes, 2 + topicBytes.Length, packetIdBytes.Length);

            // Now we have all the sizes, so we can calculate fixed header size.
            var remainingLength = variableHeaderBytes.Length + Message.Length;
            var fixedHeaderSize = CalculateFixedHeaderSize(remainingLength);

            // Finally, building the resulting full payload.
            var finalBuffer = new byte[fixedHeaderSize + remainingLength];
            finalBuffer[0] = (byte)(Type << 4);
            finalBuffer[0] += (byte)((DuplicateFlag ? 1 : 0) << 3);
            finalBuffer[0] += (byte)(((byte)QosLevel) << 1);
            finalBuffer[0] += (byte)((RetainFlag ? 1 : 0) << 0);
            EncodeRemainingLength(remainingLength, finalBuffer, 1);
            Array.Copy(variableHeaderBytes, 0, finalBuffer, fixedHeaderSize, variableHeaderBytes.Length);
            Array.Copy(Message, 0, finalBuffer, fixedHeaderSize + variableHeaderBytes.Length, Message.Length);

            return finalBuffer;
        }

        public static bool TryParse(byte flags, byte[] variableHeaderAndPayloadBytes, out PublishPacket parsedPacket) {
            var isOk = true;
            parsedPacket = new PublishPacket();

            // topic name
            var topicUtf8Length = (variableHeaderAndPayloadBytes[0] << 8) + variableHeaderAndPayloadBytes[1];


            //topicUtf8 = new byte[topicUtf8Length];
            //Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
            //index += topicUtf8Length;

            parsedPacket.Topic = new string(Encoding.UTF8.GetChars(variableHeaderAndPayloadBytes, 2, topicUtf8Length));

            // read QoS level from fixed header
            parsedPacket.QosLevel = (QosLevel)((flags & 0b0110) >> 1);
            // check wrong QoS level (both bits can't be set 1)
            if (parsedPacket.QosLevel > QosLevel.ExactlyOnce) {
                isOk = false;
            }

            // read DUP flag from fixed header
            parsedPacket.DuplicateFlag = (flags >> 3) == 0x01;

            // read retain flag from fixed header
            parsedPacket.RetainFlag = ((flags & 0x01) >> 0) == 0x01;

            // Packet id is valid only with QOS level 1 or QOS level 2
            if ((parsedPacket.QosLevel == QosLevel.AtLeastOnce) || (parsedPacket.QosLevel == QosLevel.ExactlyOnce)) {
                // message id
                parsedPacket.PacketId = (ushort)((variableHeaderAndPayloadBytes[topicUtf8Length + 2] << 8) + variableHeaderAndPayloadBytes[topicUtf8Length + 2 + 1]);
            }

            // get payload with message data
            var payloadSize = variableHeaderAndPayloadBytes.Length - topicUtf8Length - 2;
            if ((parsedPacket.QosLevel == QosLevel.AtLeastOnce) || (parsedPacket.QosLevel == QosLevel.ExactlyOnce)) {
                payloadSize -= 2;
            }

            var payloadOffset = variableHeaderAndPayloadBytes.Length - payloadSize;
            parsedPacket.Message = new byte[payloadSize];

            Array.Copy(variableHeaderAndPayloadBytes, payloadOffset, parsedPacket.Message, 0, payloadSize);

            return isOk;
        }
    }
}
