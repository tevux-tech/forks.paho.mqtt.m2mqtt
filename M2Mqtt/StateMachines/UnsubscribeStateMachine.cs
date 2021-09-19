using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class UnsubscribeStateMachine {
        private ArrayList _unacknowledgedMessages = new ArrayList();
        private double _lastAck;
        private MqttClient _client;

        public bool IsServerLost { get; private set; }

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            if (currentTime - _lastAck > _client.DelayOnRetry) {
                if (_unacknowledgedMessages.Count > 0) {
                    Trace.WriteLine(TraceLevel.Queuing, $"Cleaning unacknowledged Unsubscribe message.");
#warning Server did not acknowledged all unsubscribe messages. Is this a protocol violation?..
                    lock (_unacknowledgedMessages.SyncRoot) {
                        _unacknowledgedMessages.Clear();
                    }
                }
            }
        }

        public void Unsubscribe(MqttMsgUnsubscribe message) {
            lock (_unacknowledgedMessages.SyncRoot) {
                _unacknowledgedMessages.Add(message);
            }

            _client.Send(message);
            Trace.WriteLine(TraceLevel.Frame, $"                UnsubA-> {message.MessageId.ToString("X4")}");
        }

        public void ProcessMessage(MqttMsgUnsuback message) {
            Trace.WriteLine(TraceLevel.Frame, $"                <-Unsubs {message.MessageId.ToString("X4")}");

            _lastAck = Helpers.GetCurrentTime();

            lock (_unacknowledgedMessages.SyncRoot) {
                MqttMsgUnsubscribe foundMessage = null;
                foreach (MqttMsgUnsubscribe unsubscribeMessage in _unacknowledgedMessages) {
                    if (unsubscribeMessage.MessageId == message.MessageId) {
                        foundMessage = unsubscribeMessage;
                        break;
                    }
                }

                if (foundMessage != null) {
                    _unacknowledgedMessages.Remove(foundMessage);
#warning of course, that's not the place to raise events.
                    _client.OnMqttMsgUnsubscribed(message.MessageId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"Rogue UnsubAck message for MessageId {message.MessageId.ToString("X4")}");
#warning Rogue UnsubAck message?..
                }
            }
        }
    }
}
