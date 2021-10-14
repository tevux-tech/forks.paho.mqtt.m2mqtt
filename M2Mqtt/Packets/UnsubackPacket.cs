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
    /// Class for UNSUBACK packet from broker to client. See section 3.11.
    /// </summary>
    internal class UnsubackPacket : ControlPacketBase {
        public UnsubackPacket() {
            Type = PacketTypes.Unsuback;
        }

        public override byte[] GetBytes() {
            // Not relevant for the client side, so just leaving it empty.
            return new byte[0];
        }

        public static bool TryParse(byte[] variableHeaderBytes, out UnsubackPacket parsedPacket) {
            var isOk = true;
            parsedPacket = new UnsubackPacket {
                // Bytes 1-2: Packet Identifier. Can be anything.
                PacketId = (ushort)((variableHeaderBytes[0] << 8) + variableHeaderBytes[1])
            };

            return isOk;
        }

        public override string ToString() {
            return GetTraceString("UNSUBACK", new object[] { "packetId" }, new object[] { PacketId });
        }
    }
}
