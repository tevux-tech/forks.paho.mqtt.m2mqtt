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
    /// Class for SUBSCRIBE packet from client to broker. See section 3.22.
    /// </summary>
    internal class SubscribePacket : ControlPacketBase {

        public string Topic { get; private set; }

        public QosLevel QosLevel { get; private set; }

        internal SubscribePacket() {
            Type = PacketTypes.Subscribe;
        }

        public SubscribePacket(string topic, QosLevel qosLevel) : this() {
            if (string.IsNullOrEmpty(topic)) { throw new ArgumentException($"Argument '{nameof(topic)}' has to be a valid non-empty string", nameof(topic)); }
            if (Encoding.UTF8.GetByteCount(topic) > 65535) { throw new ArgumentException("Topic is too long. Maximum length is 65535."); }

            PacketId = GetNewPacketId();
            Topic = topic;
            QosLevel = qosLevel;
        }

        public override byte[] GetBytes() {
            // Currently this class was simplified to contain a single topic, although protocol supports multiple topics per packet.
            // This greatly simplifies buffer construction.

            // Payload section.
            var topicBytes = Encoding.UTF8.GetBytes(Topic);
            var payloadSize = 2 + topicBytes.Length + 1;
            var payloadBytes = new byte[payloadSize];
            payloadBytes[0] = (byte)(topicBytes.Length >> 8);
            payloadBytes[1] = (byte)(topicBytes.Length & 0xFF);
            Array.Copy(topicBytes, 0, payloadBytes, 2, topicBytes.Length);
            payloadBytes[2 + topicBytes.Length] = (byte)QosLevel;

            // Variable header section.
            var variableHeaderBytes = new byte[2];
            variableHeaderBytes[0] = (byte)(PacketId >> 8);
            variableHeaderBytes[1] = (byte)(PacketId & 0xFF);

            // Now we have all the sizes, so we can calculate fixed header size.
            var remainingLength = variableHeaderBytes.Length + payloadBytes.Length;
            var fixedHeaderSize = CalculateFixedHeaderSize(remainingLength);

            // Finally, building the resulting full payload.
            var finalBuffer = new byte[fixedHeaderSize + remainingLength];
            finalBuffer[0] = (byte)((Type << 4) + 2);
            EncodeRemainingLength(remainingLength, finalBuffer, 1);
            Array.Copy(variableHeaderBytes, 0, finalBuffer, fixedHeaderSize, variableHeaderBytes.Length);
            Array.Copy(payloadBytes, 0, finalBuffer, fixedHeaderSize + variableHeaderBytes.Length, payloadBytes.Length);

            return finalBuffer;
        }

        public override string ToString() {
            return GetTraceString("SUBSCRIBE", new object[] { "packetId", "topic", "qosLevel" }, new object[] { PacketId, Topic, QosLevel });
        }
    }
}
