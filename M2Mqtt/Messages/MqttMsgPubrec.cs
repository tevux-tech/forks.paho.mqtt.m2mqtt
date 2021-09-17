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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PUBREC message from broker to client. See section 3.5.
    /// </summary>
    public class MqttMsgPubrec : MqttMsgBase, ISentToBroker {
        public MqttMsgPubrec() {
            Type = MessageType.PubRec;
        }

        public byte[] GetBytes() {
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // message identifier
            varHeaderSize += MessageIdSize;

            remainingLength += (varHeaderSize + payloadSize);

            var fixedHeaderSize = Helpers.CalculateFixedHeaderSize(remainingLength);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];
            buffer[index++] = (MessageType.PubRec << FixedHeader.TypeOffset) | MessageFlags.PubRec; // [v.3.1.1]

            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // get message identifier
            buffer[index++] = (byte)((MessageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(MessageId & 0x00FF); // LSB 

            return buffer;
        }

        public static bool TryParse(byte[] variableHeaderBytes, out MqttMsgPubrec parsedMessage) {
            var isOk = true;
            parsedMessage = new MqttMsgPubrec();

            // Bytes 1-2: Packet Identifier. Can be anything.
            parsedMessage.MessageId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1]);

            return isOk;
        }

        public override string ToString() {
            return Helpers.GetTraceString("PUBREC", new object[] { "messageId" }, new object[] { MessageId });
        }
    }
}
