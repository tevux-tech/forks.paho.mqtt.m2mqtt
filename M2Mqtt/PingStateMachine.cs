using System;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public class PingStateMachine {
        private bool _isWaitingForPingResponse;
        private int _requestTimestamp;
        private MqttClient _client;

        public bool IsServerLost { get; private set; }

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
                    IsServerLost = true;
                }
            }
            else {
                if (currentTime - _client.LastCommTime > MqttSettings.KeepAlivePeriod) {
                    var pingreq = new MqttMsgPingReq();

                    try {
                        _client.Send(pingreq);
                        _isWaitingForPingResponse = true;
                        _requestTimestamp = currentTime;
                    }
                    catch (Exception e) {
#warning I think this also signifies a lost server connection?..
                        Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());
                    }
                }
            }
        }

        public void ProcessMessage(MqttMsgPingResp message) {
            _isWaitingForPingResponse = false;
        }

        public void Reset() {
            IsServerLost = false;
            _isWaitingForPingResponse = false;
        }
    }
}
