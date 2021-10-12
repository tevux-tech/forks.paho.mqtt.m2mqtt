using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class ConnectStateMachine {
        private bool _isWaitingForConnack;
        private double _requestTimestamp;
        private MqttClient _client;

        public bool IsConnectionCompleted { get; private set; }
        public bool IsConnectionSuccessful { get; private set; }
        public ConnackPacket.ReturnCodes ConnectionResult { get; private set; }


        public void Initialize(MqttClient client) {
            _client = client;
            Reset();
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            if (_isWaitingForConnack) {
                if (currentTime - _requestTimestamp > _client.ConnectionOptions.KeepAlivePeriod) {
                    // Problem. Server does not respond.
                    _isWaitingForConnack = false;
                    IsConnectionCompleted = true;
                    IsConnectionSuccessful = false;
                }
            }
            else {

            }
        }

        public void ProcessPacket(ConnackPacket packet) {
            Trace.LogIncomingPacket(packet);

            _isWaitingForConnack = false;
            IsConnectionCompleted = true;
            IsConnectionSuccessful = true;
            ConnectionResult = packet.ReturnCode;
        }

        public void Connect(ConnectPacket packet) {
            Reset();

            var currentTime = Helpers.GetCurrentTime();
            _client.Send(packet);
            _isWaitingForConnack = true;
            _requestTimestamp = currentTime;
            Trace.LogOutgoingPacket(packet);
        }

        public void Reset() {
            _isWaitingForConnack = false;
            IsConnectionCompleted = false;
        }
    }
}
