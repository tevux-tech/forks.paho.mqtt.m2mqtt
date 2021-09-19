namespace Tevux.Protocols.Mqtt {
    internal class Helpers {
        /// <summary>
        /// Returns current time in seconds (since 0000-01-01).
        /// </summary>
        public static double GetCurrentTime() {
            return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerSecond;
        }
    }
}
