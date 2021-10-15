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
    /// Class for CONNACK packet from broker to client. See section 3.2.
    /// </summary>
    internal class ConnackPacket : ControlPacketBase {

        public ConnackPacket() {
            Type = PacketTypes.Conack;
        }

        public enum ReturnCodes : byte {
            Accepted = 0x00,
            RefusedUnacceptableProtocolVersion = 0x01,
            RefusedIdentifierRejected = 0x02,
            RefusedServerUnavailable = 0x03,
            RefusedBadUsernameOrPassword = 0x04,
            RefusedNotAuthorized = 0x05
        }

        public ReturnCodes ReturnCode { get; private set; }
        public bool SessionPresent { get; private set; }
        public static bool TryParse(byte[] variableHeader, out ConnackPacket parsedPacket) {
            var isOk = true;

            parsedPacket = new ConnackPacket();

            // Byte 1: ConAck flags.
            if (variableHeader[0] == 0) {
                parsedPacket.SessionPresent = false;
            }
            else if (variableHeader[0] == 1) {
                parsedPacket.SessionPresent = true;
            }
            else {
                // That's a protocol violation.
                isOk = false;
            }

            // Byte 2: Return code.
            if (variableHeader[1] < 6) {
                parsedPacket.ReturnCode = (ReturnCodes)variableHeader[1];
            }
            else {
                // That's a protocol violation.
                isOk = false;
            }

            return isOk;
        }

        public override byte[] GetBytes() {
            // Not relevant for the client side, so just leaving it empty.
            return new byte[0];
        }
    }
}
