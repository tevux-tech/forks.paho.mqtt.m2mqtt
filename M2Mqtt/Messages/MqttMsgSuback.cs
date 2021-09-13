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

using System;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace uPLibrary.Networking.M2Mqtt.Messages
{
    /// <summary>
    /// Class for SUBACK message from broker to client
    /// </summary>
    public class MqttMsgSuback : MqttMsgBase
    {
        #region Properties...

        /// <summary>
        /// List of granted QOS Levels
        /// </summary>
        public byte[] GrantedQoSLevels
        {
            get { return grantedQosLevels; }
            set { grantedQosLevels = value; }
        }

        #endregion

        // granted QOS levels
        byte[] grantedQosLevels;

        /// <summary>
        /// Constructor
        /// </summary>
        public MqttMsgSuback()
        {
            type = MessageType.SubAck;
        }

        /// <summary>
        /// Parse bytes for a SUBACK message
        /// </summary>
        /// <param name="fixedHeaderFirstByte">First fixed header byte</param>
        /// <param name="protocolVersion">Protocol Version</param>
        /// <param name="channel">Channel connected to the broker</param>
        /// <returns>SUBACK message instance</returns>
        public static MqttMsgSuback Parse(byte fixedHeaderFirstByte, byte protocolVersion, IMqttNetworkChannel channel)
        {
            byte[] buffer;
            int index = 0;
            MqttMsgSuback msg = new MqttMsgSuback();

            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
            {
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MQTT_MSG_SUBACK_FLAG_BITS)
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);
            }

            // get remaining length and allocate buffer
            int remainingLength = decodeRemainingLength(channel);
            buffer = new byte[remainingLength];

            // read bytes from socket...
            channel.Receive(buffer);

            // message id
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);

            // payload contains QoS levels granted
            msg.grantedQosLevels = new byte[remainingLength - MessageIdSize];
            int qosIdx = 0;
            do
            {
                msg.grantedQosLevels[qosIdx++] = buffer[index++];
            } while (index < remainingLength);

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion)
        {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] buffer;
            int index = 0;

            // message identifier
            varHeaderSize += MessageIdSize;

            int grantedQosIdx = 0;
            for (grantedQosIdx = 0; grantedQosIdx < grantedQosLevels.Length; grantedQosIdx++)
            {
                payloadSize++;
            }

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do
            {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                buffer[index++] = (MessageType.SubAck << MSG_TYPE_OFFSET) | MQTT_MSG_SUBACK_FLAG_BITS; // [v.3.1.1]
            else
                buffer[index++] = (byte)(MessageType.SubAck << MSG_TYPE_OFFSET);

            // encode remaining length
            index = encodeRemainingLength(remainingLength, buffer, index);

            // message id
            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB

            // payload contains QoS levels granted
            for (grantedQosIdx = 0; grantedQosIdx < grantedQosLevels.Length; grantedQosIdx++)
            {
                buffer[index++] = grantedQosLevels[grantedQosIdx];
            }

            return buffer;
        }

        public override string ToString()
        {
#if TRACE
            return GetTraceString(
                "SUBACK",
                new object[] { "messageId", "grantedQosLevels" },
                new object[] { messageId, grantedQosLevels });
#else
            return base.ToString();
#endif
        }
    }
}
