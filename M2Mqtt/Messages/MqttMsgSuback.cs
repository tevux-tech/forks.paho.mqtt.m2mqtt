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

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for SUBACK message from broker to client. See section 3.9.
    /// </summary>
    internal class MqttMsgSuback : MqttMsgBase {
        public GrantedQosLevel[] GrantedQosLevels { get; private set; }

        public MqttMsgSuback() {
            Type = MessageType.SubAck;
        }

        public override byte[] GetBytes() {
            // Not relevant for the client side, so just leaving it empty.
            return new byte[0];
        }

        public static bool TryParse(byte[] variableHeaderBytes, byte[] payloadBytes, out MqttMsgSuback parsedMessage) {
            var isOk = true;
            parsedMessage = new MqttMsgSuback();

            // Bytes 1-2: Packet Identifier. Can be anything.
            parsedMessage.MessageId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1]);

            // Remaining bytes: QoS levels granted.
            parsedMessage.GrantedQosLevels = new GrantedQosLevel[payloadBytes.Length];
            for (var i = 0; i < payloadBytes.Length; i++) {
                if ((payloadBytes[i] & 0x80) == 0x80) {
                    // QoS was not granted for that topic, but that's a valid payload.
                    parsedMessage.GrantedQosLevels[i] = (GrantedQosLevel)payloadBytes[i];
                }
                else if ((payloadBytes[i] & 0x03) < 0x03) {
                    // QoS was granted.
                    parsedMessage.GrantedQosLevels[i] = (GrantedQosLevel)payloadBytes[i];
                }
                else {
                    // That's a protocol violation.
                    isOk = false;
                }
            }

            return isOk;
        }

        public override string ToString() {
            return GetTraceString("SUBACK", new object[] { "messageId", "grantedQosLevels" }, new object[] { MessageId, GrantedQosLevels });
        }
    }
}
