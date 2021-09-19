using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using static uPLibrary.Networking.M2Mqtt.Messages.MqttMsgConnack;

namespace uPLibrary.Networking.M2Mqtt {
    internal class ConnectStateMachine {
        private bool _isWaitingForConnack;
        private double _requestTimestamp;
        private MqttClient _client;

        public bool IsConnectionCompleted { get; private set; }
        public ReturnCodes ConnectionResult { get; private set; }


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
                }
            }
            else {

            }
        }

        public void ProcessMessage(MqttMsgConnack message) {
            Trace.WriteLine(TraceLevel.Frame, "<-ConAck");

            _isWaitingForConnack = false;
            IsConnectionCompleted = true;
            ConnectionResult = message.ReturnCode;
        }

        public void Connect(MqttMsgConnect message) {
            var currentTime = Helpers.GetCurrentTime();
            _client.Send(message);
            _isWaitingForConnack = true;
            _requestTimestamp = currentTime;
            Trace.WriteLine(TraceLevel.Frame, "Connec->");
        }

        public void Reset() {
            _isWaitingForConnack = false;
            IsConnectionCompleted = false;
        }
    }
}
