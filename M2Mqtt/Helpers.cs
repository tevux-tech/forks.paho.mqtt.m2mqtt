using System.Text;

namespace uPLibrary.Networking.M2Mqtt {
    public class Helpers {
        /// <summary>
        /// Calculates the size of the fixed header, which depends on the remaining length. See section 2.2.3.
        /// </summary>
        public static int CalculateFixedHeaderSize(int remainingLength) {
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
        public static bool TryDecodeRemainingLength(IMqttNetworkChannel channel, out int remainingLength) {
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
        /// Encode remaining length and insert it into message buffer
        /// </summary>
        /// <param name="index">Index from which insert encoded value into buffer</param>
        /// <returns>Index updated</returns>
        public static int EncodeRemainingLength(int remainingLength, byte[] buffer, int index) {
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
        /// Returns a string representation of the message for tracing
        /// </summary>
        public static string GetTraceString(string name, object[] fieldNames, object[] fieldValues) {
            object GetStringObject(object value) {
                var binary = value as byte[];
                if (binary != null) {
                    var hexChars = "0123456789ABCDEF";
                    var sb = new StringBuilder(binary.Length * 2);
                    for (var i = 0; i < binary.Length; ++i) {
                        sb.Append(hexChars[binary[i] >> 4]);
                        sb.Append(hexChars[binary[i] & 0x0F]);
                    }

                    return sb.ToString();
                }

                var list = value as object[];
                if (list != null) {
                    var sb = new StringBuilder();
                    sb.Append('[');
                    for (var i = 0; i < list.Length; ++i) {
                        if (i > 0) {
                            sb.Append(',');
                        }

                        sb.Append(list[i]);
                    }
                    sb.Append(']');

                    return sb.ToString();
                }

                return value;
            }

            var outputBuilder = new StringBuilder();
            outputBuilder.Append(name);

            if ((fieldNames != null) && (fieldValues != null)) {
                outputBuilder.Append("(");
                var addComma = false;
                for (var i = 0; i < fieldValues.Length; i++) {
                    if (fieldValues[i] != null) {
                        if (addComma) {
                            outputBuilder.Append(",");
                        }

                        outputBuilder.Append(fieldNames[i]);
                        outputBuilder.Append(":");
                        outputBuilder.Append(GetStringObject(fieldValues[i]));
                        addComma = true;
                    }
                }
                outputBuilder.Append(")");
            }

            return outputBuilder.ToString();
        }
    }
}
