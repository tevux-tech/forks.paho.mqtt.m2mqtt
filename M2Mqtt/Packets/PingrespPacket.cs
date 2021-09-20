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
    /// Class for PINGRESP packet from client to broker. See section 3.13.
    /// </summary>
    internal class PingrespPacket : ControlPacketBase {
        public PingrespPacket() {
            Type = PacketTypes.Pingresp;
        }

        public override byte[] GetBytes() {
            // Not relevant for the client side, so just leaving it empty.
            return new byte[0];
        }

        public static bool TryParse(out PingrespPacket parsedPacket) {
            // Nothing to parse here.
            parsedPacket = new PingrespPacket();

            return true;
        }

        public override string ToString() {
            return GetTraceString("PINGRESP", null, null);
        }
    }
}
