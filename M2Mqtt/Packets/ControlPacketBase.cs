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
    /// Base class for all MQTT packets.
    /// </summary>
    internal abstract class ControlPacketBase {
        public class PacketTypes {
            public const byte Connect = 0x01;
            public const byte Conack = 0x02;
            public const byte Publish = 0x03;
            public const byte Puback = 0x04;
            public const byte Pubrec = 0x05;
            public const byte Pubrel = 0x06;
            public const byte Pubcomp = 0x07;
            public const byte Subscribe = 0x08;
            public const byte Suback = 0x09;
            public const byte Unsubscribe = 0x0A;
            public const byte Unsuback = 0x0B;
            public const byte Pingreq = 0x0C;
            public const byte Pingresp = 0x0D;
            public const byte Disconnect = 0x0E;

            public static string GetShortName(byte type) {
                switch (type) {
                    case Connect:
                        return nameof(Connect).Substring(0, 6);

                    case Conack:
                        return nameof(Conack).Substring(0, 6);

                    case Publish:
                        return nameof(Publish).Substring(0, 6);

                    case Puback:
                        return nameof(Puback).Substring(0, 6);

                    case Pubrec:
                        return nameof(Pubrec).Substring(0, 6);

                    case Pubrel:
                        return nameof(Pubrel).Substring(0, 6);

                    case Pubcomp:
                        return nameof(Pubcomp).Substring(0, 6);

                    case Subscribe:
                        return nameof(Subscribe).Substring(0, 6);

                    case Suback:
                        return nameof(Suback).Substring(0, 6);

                    case Unsubscribe:
                        return nameof(Unsubscribe).Substring(0, 6);

                    case Unsuback:
                        return nameof(Unsuback).Substring(0, 6);

                    case Pingreq:
                        return "Pngreq";

                    case Pingresp:
                        return "Pngres";

                    case Disconnect:
                        return nameof(Disconnect).Substring(0, 6);

                    default:
                        return "Undefi";
                }
            }

            public static string GetShortBlank() {
                return "      ";
            }
        }

        private static ushort _packetIdCounter = 0;

        public byte Type { get; protected set; }

        public ushort PacketId { get; protected set; }

        public abstract byte[] GetBytes();

        public string GetShortName() {
            return PacketTypes.GetShortName(Type);
        }

        /// <summary>
        /// Calculates the size of the fixed header, which depends on the remaining length. See section 2.2.3.
        /// </summary>
        protected static int CalculateFixedHeaderSize(int remainingLength) {
            var fixedHeaderSize = 1;
            var temp = remainingLength;

            do {
                fixedHeaderSize++;
                temp /= 128;
            } while (temp > 0);

            return fixedHeaderSize;
        }

        /// <summary>
        /// Decode remaining length reading bytes from socket
        /// </summary>
        internal static bool TryDecodeRemainingLength(IMqttNetworkChannel channel, out int remainingLength) {
            var isOk = true;
            var multiplier = 1;
            remainingLength = 0;
            var nextByte = new byte[1];
            int digit;
            do {
                // next digit from stream
                isOk &= channel.TryReceive(nextByte);
                digit = nextByte[0];
                remainingLength += ((digit & 127) * multiplier);
                multiplier *= 128;
            } while ((digit & 128) != 0);

            return isOk;
        }

        /// <summary>
        /// Encode remaining length and insert it into packet buffer
        /// </summary>
        /// <param name="index">Index from which insert encoded value into buffer</param>
        protected static void EncodeRemainingLength(int remainingLength, byte[] buffer, int index) {
            do {
                var digit = remainingLength % 128;
                remainingLength /= 128;
                if (remainingLength > 0) {
                    digit |= 0x80;
                }
                buffer[index++] = (byte)digit;
            } while (remainingLength > 0);
        }

        /// <summary>
        /// Generates the next packet identifier.
        /// </summary>
        protected static ushort GetNewPacketId() {
            // if 0 or max UInt16, it becomes 1 (first valid packet ID)
            _packetIdCounter = ((_packetIdCounter % ushort.MaxValue) != 0) ? (ushort)(_packetIdCounter + 1) : (ushort)1;
            return _packetIdCounter;
        }

        /// <summary>
        /// Returns a string representation of the message for tracing.
        /// </summary>
        public static string GetTraceString(string name, object[] fieldNames, object[] fieldValues) {
            static object GetStringObject(object value) {
                if (value is byte[] binary) {
                    var hexChars = "0123456789ABCDEF";
                    var sb = new System.Text.StringBuilder(binary.Length * 2);
                    for (var i = 0; i < binary.Length; ++i) {
                        _ = sb.Append(hexChars[binary[i] >> 4]);
                        _ = sb.Append(hexChars[binary[i] & 0x0F]);
                    }

                    return sb.ToString();
                }

                if (value is object[] list) {
                    var sb = new System.Text.StringBuilder();
                    _ = sb.Append('[');
                    for (var i = 0; i < list.Length; ++i) {
                        if (i > 0) {
                            _ = sb.Append(',');
                        }

                        _ = sb.Append(list[i]);
                    }
                    _ = sb.Append(']');

                    return sb.ToString();
                }

                return value;
            }

            var outputBuilder = new System.Text.StringBuilder();
            _ = outputBuilder.Append(name);

            if ((fieldNames != null) && (fieldValues != null)) {
                _ = outputBuilder.Append('(');
                var addComma = false;
                for (var i = 0; i < fieldValues.Length; i++) {
                    if (fieldValues[i] != null) {
                        if (addComma) {
                            _ = outputBuilder.Append(',');
                        }

                        _ = outputBuilder.Append(fieldNames[i]);
                        _ = outputBuilder.Append(':');
                        _ = outputBuilder.Append(GetStringObject(fieldValues[i]));
                        addComma = true;
                    }
                }
                _ = outputBuilder.Append(')');
            }

            return outputBuilder.ToString();
        }
    }
}
