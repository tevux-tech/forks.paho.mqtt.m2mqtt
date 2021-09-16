using System;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public class SubscribeStateMachine {
        private ArrayList _unacknowledgedMessages = new ArrayList();
        private int _lastAck;
        private MqttClient _client;

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Environment.TickCount;

            if (currentTime - _lastAck > MqttSettings.KeepAlivePeriod) {
                if (_unacknowledgedMessages.Count > 0) {
                    Trace.WriteLine(TraceLevel.Queuing, $"Cleaning unacknowledged Subscribe message.");

#warning Server did not acknowledged all Subscribe messages. Is this a protocol violation?..
                    lock (_unacknowledgedMessages.SyncRoot) {
                        _unacknowledgedMessages.Clear();
                    }
                }
            }
        }

        public void Subscribe(MqttMsgSubscribe message) {
            lock (_unacknowledgedMessages.SyncRoot) {
                _unacknowledgedMessages.Add(message);
            }

            _client.Send(message);
        }

        public void ProcessMessage(MqttMsgSuback message) {
            _lastAck = Environment.TickCount;

            lock (_unacknowledgedMessages.SyncRoot) {
                MqttMsgSubscribe foundMessage = null;
                foreach (MqttMsgSubscribe subscribeMessage in _unacknowledgedMessages) {
                    if (subscribeMessage.MessageId == message.MessageId) {
                        foundMessage = subscribeMessage;
                        break;
                    }
                }

                if (foundMessage != null) {
                    _unacknowledgedMessages.Remove(foundMessage);
#warning of course, that's not the place to raise events.
                    _client.OnMqttMsgSubscribed(message);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"Rogue SubAck message for MessageId {message.MessageId}");
#warning Rogue SubAck message?..
                }
            }
        }
    }
}
