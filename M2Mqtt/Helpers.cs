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
