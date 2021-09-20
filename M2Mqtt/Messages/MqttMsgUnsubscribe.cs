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
using System.Text;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for UNSUBSCRIBE message from client to broker
    /// </summary>
    internal class MqttMsgUnsubscribe : MqttMsgBase {

        public string Topic { get; private set; }

        internal MqttMsgUnsubscribe() {
            Type = MessageType.Unsubscribe;
        }

        public MqttMsgUnsubscribe(string topic) : this() {
            if (string.IsNullOrEmpty(topic)) { throw new ArgumentException($"Argument '{nameof(topic)}' has to be a valid non-empty string", nameof(topic)); }
            if (Encoding.UTF8.GetByteCount(topic) > 65535) { throw new ArgumentException("Topic is too long. Maximum length is 65535."); }

            MessageId = GetNewMessageId();
            Topic = topic;
        }

        public override byte[] GetBytes() {
            // Currently this class was simplified to contain a single topic, although protocol supports multiple topics per packet.
            // This greatly simplifies buffer construction.

            // Payload section.
            var topicBytes = Encoding.UTF8.GetBytes(Topic);
            var payloadSize = 2 + topicBytes.Length;
            var payloadBytes = new byte[payloadSize];
            payloadBytes[0] = (byte)(topicBytes.Length >> 8);
            payloadBytes[1] = (byte)(topicBytes.Length & 0xFF);
            Array.Copy(topicBytes, 0, payloadBytes, 2, topicBytes.Length);

            // Variable header section.
            var variableHeaderBytes = new byte[2];
            variableHeaderBytes[0] = (byte)(MessageId >> 8);
            variableHeaderBytes[1] = (byte)(MessageId & 0xFF);

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
            return GetTraceString("UNSUBSCRIBE", new object[] { "messageId", "topics" }, new object[] { MessageId, Topic });
        }
    }
}
