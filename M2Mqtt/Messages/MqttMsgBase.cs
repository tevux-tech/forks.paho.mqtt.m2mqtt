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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Base class for all MQTT messages
    /// </summary>
    public abstract class MqttMsgBase {
        // mask, offset and size for fixed header fields
        public class FixedHeader {
            public const byte TypeMask = 0xF0;
            public const byte TypeOffset = 0x04;
            public const byte TypeSize = 0x04;
            public const byte FlagBitsMask = 0x0F;      // [v3.1.1]
            public const byte FlagBitsOffset = 0x00;    // [v3.1.1]
            public const byte FlagBitsSize = 0x04;      // [v3.1.1]
            public const byte DuplicateFlagMask = 0x08;
            public const byte DuplicateFlagOffset = 0x03;
            public const byte DuplicateFlagSize = 0x01;
            public const byte QosLevelMask = 0x06;
            public const byte QosLevelOffset = 0x01;
            public const byte QosLevelSize = 0x02;
            public const byte RetainFlagMask = 0x01;
            public const byte RetainFlagOffset = 0x00;
            public const byte RetainFlagSize = 0x01;
        }

        public class MessageType {
            public const byte Connect = 0x01;
            public const byte ConAck = 0x02;
            public const byte Publish = 0x03;
            public const byte PubAck = 0x04;
            public const byte PubRec = 0x05;
            public const byte PubRel = 0x06;
            public const byte PubComp = 0x07;
            public const byte Subscribe = 0x08;
            public const byte SubAck = 0x09;
            public const byte Unsubscribe = 0x0A;
            public const byte UnsubAck = 0x0B;
            public const byte PingReq = 0x0C;
            public const byte PingResp = 0x0D;
            public const byte Disconnect = 0x0E;
        }

        public class MessageFlags {
            public const byte Subcribe = 0x02;
            public const byte Unsubscribe = 0x02;
        }

        // SUBSCRIBE QoS level granted failure [v3.1.1]
        public const byte QosLevelGrantedFailure = 0x80;

        public const ushort MaxTopicLength = 65535;
        public const ushort MinTopicLength = 1;
        public const byte MessageIdSize = 2;

        public byte Type { get; set; }
        public bool DupFlag { get; set; }
        public QosLevel QosLevel { get; set; }
        public bool Retain { get; set; }
        public ushort MessageId { get; set; }

        /// <summary>
        /// Encode remaining length and insert it into message buffer
        /// </summary>
        /// <param name="index">Index from which insert encoded value into buffer</param>
        /// <returns>Index updated</returns>
        protected int EncodeRemainingLength(int remainingLength, byte[] buffer, int index) {
            do {
                var digit = remainingLength % 128;
                remainingLength /= 128;
                if (remainingLength > 0) {
                    digit |= 0x80;
                }
                buffer[index++] = (byte)digit;
            } while (remainingLength > 0);

            return index;
        }

        /// <summary>
        /// Decode remaining length reading bytes from socket
        /// </summary>
        public static int DecodeRemainingLength(IMqttNetworkChannel channel) {
            var multiplier = 1;
            var value = 0;
            var nextByte = new byte[1];
            int digit;
            do {
                // next digit from stream
                channel.Receive(nextByte);
                digit = nextByte[0];
                value += ((digit & 127) * multiplier);
                multiplier *= 128;
            } while ((digit & 128) != 0);

            return value;
        }
    }
}
