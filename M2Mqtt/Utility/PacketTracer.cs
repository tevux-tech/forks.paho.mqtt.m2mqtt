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
   Simonas Greicius - conversion to PacketTracer
*/

using System;

namespace Tevux.Protocols.Mqtt.Utility {
    public static class PacketTracer {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        internal static void LogIncomingPacket(ControlPacketBase packet, bool isRogue = false) {
            var categoryIndent = "        " + ControlPacketBase.PacketTypes.GetShortBlank() + "   ";

            switch (packet.Type) {
                case ControlPacketBase.PacketTypes.Conack:
                    categoryIndent += "                                                ";
                    break;

                case ControlPacketBase.PacketTypes.Suback:
                case ControlPacketBase.PacketTypes.Unsuback:
                    categoryIndent += "                        ";
                    break;

                case ControlPacketBase.PacketTypes.Pingreq:
                case ControlPacketBase.PacketTypes.Pingresp:
                    categoryIndent = "";
                    break;
            }

            var rogueSuffix = isRogue ? "(R)" : "";

            _log.Trace($"{categoryIndent}{packet.PacketId:X4} <-{packet.GetShortName()} {rogueSuffix}");
        }
        internal static void LogOutgoingPacket(ControlPacketBase packet, int attemptNumber = 1) {
            var categoryIndent = "        ";

            switch (packet.Type) {
                case ControlPacketBase.PacketTypes.Connect:
                case ControlPacketBase.PacketTypes.Disconnect:
                    categoryIndent += "                                                ";
                    break;

                case ControlPacketBase.PacketTypes.Subscribe:
                case ControlPacketBase.PacketTypes.Unsubscribe:
                    categoryIndent += "                        ";
                    break;

                case ControlPacketBase.PacketTypes.Pingreq:
                case ControlPacketBase.PacketTypes.Pingresp:
                    categoryIndent = "";
                    break;
            }

            var attemptSuffix = attemptNumber > 1 ? attemptNumber.ToString() : "";

            _log.Trace($"{categoryIndent}{packet.GetShortName()}-> {packet.PacketId:X4} {attemptSuffix}");
        }


        public static void ReturnWith50PercentChance() {
#if DEBUG
            if ((new Random()).Next(5) > 2) { return; }
#endif
        }

    }
}
