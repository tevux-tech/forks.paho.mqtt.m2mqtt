using System;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public class UnsubscribeStateMachine {
        private ArrayList _unacknowledgedMessages = new ArrayList();
        private int _lastAck;
        private MqttClient _client;

        public bool IsServerLost { get; private set; }

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Environment.TickCount;

            if (currentTime - _lastAck > MqttSettings.KeepAlivePeriod) {
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
        }

        public void ProcessMessage(MqttMsgUnsuback message) {
            _lastAck = Environment.TickCount;

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
                    Trace.WriteLine(TraceLevel.Queuing, $"Rogue UnsubAck message for MessageId {message.MessageId}");
#warning Rogue UnsubAck message?..
                }
            }
        }
    }
}
