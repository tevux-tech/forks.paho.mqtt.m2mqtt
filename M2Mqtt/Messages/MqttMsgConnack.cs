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
    /// Class for CONNACK message from broker to client
    /// </summary>
    public class MqttMsgConnack : MqttMsgBase {

        public enum ReturnCodes : byte {
            // Section 3.2.2.3.
            Accepted = 0x00,
            RefusedUnacceptableProtocolVersion = 0x01,
            RefusedIdentifierRejected = 0x02,
            RefusedServerUnavailable = 0x03,
            RefusedBadUsernameOrPassword = 0x04,
            RefusedNotAuthorized = 0x05
        }

        /// <summary>
        /// Session present flag [v3.1.1]
        /// </summary>
        public bool SessionPresent { get; set; }

        public ReturnCodes ReturnCode { get; set; }

        public MqttMsgConnack() {
            Type = MessageType.ConAck;
        }

        public static MqttMsgConnack Parse(byte fixedHeaderFirstByte, IMqttNetworkChannel channel) {
            // Section 3.2.2.
            // Byte 1: ConAck flags.
            // Byte 2: Return code.
            byte flagsByteOffset = 0; // <-- [v3.1.1] connect acknowledge flags replace "old" topic name compression respone (not used in 3.1)
            byte flagsbyteMask = 0x01;// <-- [v3.1.1] session present flag
            byte returnCodeByteOffset = 1;

            byte[] buffer;
            var msg = new MqttMsgConnack();

            if ((fixedHeaderFirstByte & FixedHeader.FlagBitsMask) != MessageFlags.ConAck) {
                throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
            }


            // get remaining length and allocate buffer
            var remainingLength = DecodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            channel.Receive(buffer);

            // [v3.1.1] ... set session present flag ...
            msg.SessionPresent = (buffer[flagsByteOffset] & flagsbyteMask) != 0x00;

            // ...and set return code from broker
            msg.ReturnCode = (ReturnCodes)buffer[returnCodeByteOffset];

            return msg;
        }

        public override string ToString() {
            return Helpers.GetTraceString("CONNACK", new object[] { "returnCode" }, new object[] { (ReturnCodes)ReturnCode });
        }
    }
}
