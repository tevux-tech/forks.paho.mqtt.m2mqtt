using System;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    internal class SubscribeStateMachine {
        private MqttClient _client;
        private Hashtable _messagesWaitingForAck = new Hashtable();
        private ConcurrentQueue _tempList = new ConcurrentQueue();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Environment.TickCount;

            lock (_messagesWaitingForAck.SyncRoot) {
                foreach (DictionaryEntry item in _messagesWaitingForAck) {
                    var message = (MqttMsgContext)item.Value;

                    if (currentTime - message.Timestamp > MqttSettings.KeepAlivePeriod) {
                        _client.Send((ISentToBroker)message.Message);
                        message.Attempt++;
                    }

                    if (message.Attempt >= MqttSettings.AttemptsRetry) {
                        _tempList.Enqueue(item.Key);
                        Trace.WriteLine(TraceLevel.Queuing, $"                Subscribe message {message.Message.MessageId} could no be sent, even after retries.");
                    }
                }
            }

            var areThereMessageToRemove = true;
            while (areThereMessageToRemove) {
                if (_tempList.TryDequeue(out var item)) {
                    Trace.WriteLine(TraceLevel.Queuing, $"                Cleaning unacknowledged Subscribe message {item.ToString()}.");
                    lock (_messagesWaitingForAck.SyncRoot) {
                        _messagesWaitingForAck.Remove(item);
                    }
                }
                else {
                    areThereMessageToRemove = false;
                }
            }
        }

        public void Subscribe(MqttMsgSubscribe message) {
            var currentTime = Environment.TickCount;

            lock (_messagesWaitingForAck.SyncRoot) {
                _messagesWaitingForAck.Add(message.MessageId, new MqttMsgContext() { Message = message, Attempt = 1, Timestamp = currentTime });
            }

            _client.Send(message);
            Trace.WriteLine(TraceLevel.Frame, $"                Subscr-> {message.MessageId.ToString("X4")}");
        }

        public void ProcessMessage(MqttMsgSuback message) {
            Trace.WriteLine(TraceLevel.Frame, $"                <-SubAck {message.MessageId.ToString("X4")}");

            lock (_messagesWaitingForAck.SyncRoot) {
                if (_messagesWaitingForAck.Contains(message.MessageId)) {
                    _messagesWaitingForAck.Remove(message.MessageId);
#warning of course, that's not the place to raise events.
                    _client.OnMqttMsgSubscribed(message);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"                Rogue SubAck message for MessageId {message.MessageId.ToString("X4")}");
#warning Rogue SubAck message, what do I do now?..
                }
            }
        }
    }
}
