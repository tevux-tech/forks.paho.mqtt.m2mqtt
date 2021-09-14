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

using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PINGRESP message from client to broker
    /// </summary>
    public class MqttMsgPingResp : MqttMsgBase {
        public MqttMsgPingResp() {
            type = MessageType.PingResp;
        }

        public static MqttMsgPingResp Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel) {
            // Section 3.13.
            var msg = new MqttMsgPingResp();

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1) {
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & FixedHeader.FlagBitsMask) != MessageFlags.PingResp) {
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
                }
            }

            // already know remaininglength is zero (MQTT specification),
            // so it isn't necessary to read other data from socket
            var remainingLength = DecodeRemainingLength(channel);

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion) {
            // Not needed for the client side.
            return new byte[0];
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("PINGRESP", null, null);
#else
            return base.ToString();
#endif
        }
    }
}
