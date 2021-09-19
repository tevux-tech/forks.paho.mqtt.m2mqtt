using System;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    internal class PingStateMachine {
        private bool _isWaitingForPingResponse;
        private int _requestTimestamp;
        private MqttClient _client;

        public bool IsBrokerAlive { get; private set; } = true;

        public void Initialize(MqttClient client) {
            _client = client;
            Reset();
        }

        public void Tick() {
            var currentTime = Environment.TickCount;

            if (_isWaitingForPingResponse) {
                if (currentTime - _requestTimestamp > MqttSettings.KeepAlivePeriod) {
                    // Problem. Server does not respond.
                    _isWaitingForPingResponse = false;
                    IsBrokerAlive = false;
                }
            }
            else {
                // Keep alive period equals zero means turning off keep alive mechanism.
                if ((currentTime - _client.LastCommTime > MqttSettings.KeepAlivePeriod) && (_client.ConnectionOptions.KeepAlivePeriod > 0)) {
                    var pingreq = new MqttMsgPingReq();
                    _client.Send(pingreq);
                    Trace.WriteLine(TraceLevel.Frame, "                        PngReq->");
                    _isWaitingForPingResponse = true;
                    _requestTimestamp = currentTime;
                }
            }
        }

        public void ProcessMessage(MqttMsgPingResp message) {
            Trace.WriteLine(TraceLevel.Frame, "                        <-PngRes");
            IsBrokerAlive = true;
            _isWaitingForPingResponse = false;
        }

        public void Reset() {
            IsBrokerAlive = true;
            _isWaitingForPingResponse = false;
        }
    }
}
