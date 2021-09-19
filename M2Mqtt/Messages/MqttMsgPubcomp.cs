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
    /// Class for PUBCOMP message from broker to client. See section 3.7.
    /// </summary>
    internal class MqttMsgPubcomp : MqttMsgBase {
        public MqttMsgPubcomp() {
            Type = MessageType.PubComp;
        }

        public override byte[] GetBytes() {
            // PubComp packet is always 4 bytes long.
            var buffer = new byte[4];

            // Fixed header is fixed, no variables here.
            buffer[0] = (byte)(Type << 4);
            buffer[1] = 2;

            // Variable header is always two bytes - packet ID.
            buffer[2] = (byte)(MessageId >> 8);
            buffer[3] = (byte)(MessageId & 0xFF);

            return buffer;
        }

        public static bool TryParse(byte[] variableHeaderBytes, out MqttMsgPubcomp parsedMessage) {
            var isOk = true;
            parsedMessage = new MqttMsgPubcomp();

            // Bytes 1-2: Packet Identifier. Can be anything.
            parsedMessage.MessageId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1]);

            return isOk;
        }

        public override string ToString() {
            return Helpers.GetTraceString("PUBCOMP", new object[] { "messageId" }, new object[] { MessageId });
        }
    }
}
