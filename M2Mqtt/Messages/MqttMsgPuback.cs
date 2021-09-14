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
    /// Class for PUBACK message from broker to client
    /// </summary>
    public class MqttMsgPuback : MqttMsgBase, ISentToBroker {
        public MqttMsgPuback() {
            Type = MessageType.PubAck;
        }
        public static MqttMsgPuback Parse(byte fixedHeaderFirstByte, IMqttNetworkChannel channel) {
            byte[] buffer;
            var index = 0;
            var msg = new MqttMsgPuback();

            // [v3.1.1] check flag bits
            if ((fixedHeaderFirstByte & FixedHeader.FlagBitsMask) != MessageFlags.PubAck) {
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

            return msg;
        }

        public byte[] GetBytes() {
# warning I think I overdid it here

            // Not needed for the client side.
            return new byte[0];
        }
        public override string ToString() {
#if TRACE
            return GetTraceString("PUBACK", new object[] { "messageId" }, new object[] { MessageId });
#else
            return base.ToString();
#endif
        }
    }
}
