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
    }
}
