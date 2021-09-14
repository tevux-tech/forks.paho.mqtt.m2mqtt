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
            type = MessageType.SubAck;
        }

        public static MqttMsgSuback Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel) {
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
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);

            // payload contains QoS levels granted
            msg.GrantedQoSLevels = new byte[remainingLength - MessageIdSize];
            var qosIdx = 0;
            do {
                msg.GrantedQoSLevels[qosIdx++] = buffer[index++];
            } while (index < remainingLength);

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion) {
            var fixedHeaderSize = 0;
            var varHeaderSize = 0;
            var payloadSize = 0;
            var remainingLength = 0;
            byte[] buffer;
            var index = 0;

            // message identifier
            varHeaderSize += MessageIdSize;

            var grantedQosIdx = 0;
            for (grantedQosIdx = 0; grantedQosIdx < GrantedQoSLevels.Length; grantedQosIdx++) {
                payloadSize++;
            }

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            var temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            buffer[index++] = (MessageType.SubAck << FixedHeader.TypeOffset) | MessageFlags.SubAck; // [v.3.1.1]


            // encode remaining length
            index = EncodeRemainingLength(remainingLength, buffer, index);

            // message id
            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB

            // payload contains QoS levels granted
            for (grantedQosIdx = 0; grantedQosIdx < GrantedQoSLevels.Length; grantedQosIdx++) {
                buffer[index++] = GrantedQoSLevels[grantedQosIdx];
            }

            return buffer;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("SUBACK", new object[] { "messageId", "grantedQosLevels" }, new object[] { messageId, GrantedQoSLevels });
#else
            return base.ToString();
#endif
        }
    }
}
