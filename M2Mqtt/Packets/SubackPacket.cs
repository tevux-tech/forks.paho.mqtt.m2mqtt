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

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for SUBACK packet from broker to client. See section 3.9.
    /// </summary>
    internal class SubackPacket : ControlPacketBase {
        public GrantedQosLevel GrantedQosLevel { get; private set; }

        public SubackPacket() {
            Type = PacketTypes.Suback;
        }

        public override byte[] GetBytes() {
            // Not relevant for the client side, so just leaving it empty.
            return new byte[0];
        }

        public static bool TryParse(byte[] variableHeaderBytes, byte[] payloadBytes, out SubackPacket parsedPacket) {
            var isOk = true;
            parsedPacket = new SubackPacket();

            // Bytes 1-2: Packet Identifier. Can be anything.
            parsedPacket.PacketId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1]);

            // Remaining bytes: QoS level granted. MQTT supports multiple topics per packet,
            // but this library simplifies everything to a single topic per packet.
            if ((payloadBytes[0] & 0x80) == 0x80) {
                // QoS was not granted for that topic, but that's a valid payload.
                parsedPacket.GrantedQosLevel = (GrantedQosLevel)payloadBytes[0];
            }
            else if ((payloadBytes[0] & 0x03) < 0x03) {
                // QoS was granted.
                parsedPacket.GrantedQosLevel = (GrantedQosLevel)payloadBytes[0];
            }
            else {
                // That's a protocol violation.
                isOk = false;
            }

            return isOk;
        }

        public override string ToString() {
            return GetTraceString("SUBACK", new object[] { "packetId", "grantedQosLevel" }, new object[] { PacketId, GrantedQosLevel });
        }
    }
}
