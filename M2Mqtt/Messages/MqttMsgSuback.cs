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
    /// Class for SUBACK message from broker to client
    /// </summary>
    public class MqttMsgSuback : MqttMsgBase {
        public byte[] GrantedQoSLevels { get; set; }

        public MqttMsgSuback() {
            Type = MessageType.SubAck;
        }

        public static MqttMsgSuback Parse(byte fixedHeaderFirstByte, IMqttNetworkChannel channel) {
            byte[] buffer;
            var index = 0;
            var msg = new MqttMsgSuback();

            // [v3.1.1] check flag bits
            if ((fixedHeaderFirstByte & FixedHeader.FlagBitsMask) != MessageFlags.SubAck) {
                throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
            }

            // get remaining length and allocate buffer
            var remainingLength = DecodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            channel.Receive(buffer);

            // message id
            msg.MessageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.MessageId |= (buffer[index++]);

            // payload contains QoS levels granted
            msg.GrantedQoSLevels = new byte[remainingLength - MessageIdSize];
            var qosIdx = 0;
            do {
                msg.GrantedQoSLevels[qosIdx++] = buffer[index++];
            } while (index < remainingLength);

            return msg;
        }

        public override string ToString() {
            return Helpers.GetTraceString("SUBACK", new object[] { "messageId", "grantedQosLevels" }, new object[] { MessageId, GrantedQoSLevels });
        }
    }
}
