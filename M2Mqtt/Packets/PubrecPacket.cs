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
    /// Class for PUBREC packet from broker to client. See section 3.5.
    /// </summary>
    internal class PubrecPacket : ControlPacketBase {
        public PubrecPacket(ushort packetId) {
            Type = PacketTypes.Pubrec;
            PacketId = packetId;
        }

        public override byte[] GetBytes() {
            // Pubrec packet is always 4 bytes long.
            var buffer = new byte[4];

            // Fixed header is fixed, no variables here.
            buffer[0] = (byte)(Type << 4);
            buffer[1] = 2;

            // Variable header is always two bytes - packet ID.
            buffer[2] = (byte)(PacketId >> 8);
            buffer[3] = (byte)(PacketId & 0xFF);

            return buffer;
        }

        public static bool TryParse(byte[] variableHeaderBytes, out PubrecPacket parsedPacket) {
            // Bytes 1-2: Packet Identifier. Can be anything.
            var packetId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1]);
            parsedPacket = new PubrecPacket(packetId);

            return true;
        }
    }
}
