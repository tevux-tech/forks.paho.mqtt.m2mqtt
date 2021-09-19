using System;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    internal class OutgoingPublishStateMachine {
        private MqttClient _client;
        private readonly ConcurrentQueue _tempList = new ConcurrentQueue();

        private readonly Hashtable _messagesWaitingForQoS1PubAck = new Hashtable();
        private readonly Hashtable _messagesWaitingForQoS2Pubrec = new Hashtable();
        private readonly Hashtable _messagesWaitingForQoS2Pubcomp = new Hashtable();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            ResendAndClean(_messagesWaitingForQoS1PubAck);
            ResendAndClean(_messagesWaitingForQoS2Pubrec);
            ResendAndClean(_messagesWaitingForQoS2Pubcomp);
        }

        public void Publish(MqttMsgPublish message) {
            var currentTime = Environment.TickCount;

            if (message.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best messages - just sending and waiting for no responses!
            }
            else if (message.QosLevel == QosLevel.AtLeastOnce) {
                lock (_messagesWaitingForQoS1PubAck.SyncRoot) {
                    _messagesWaitingForQoS1PubAck.Add(message.MessageId, new MqttMsgContext() { Message = message, Attempt = 1, Timestamp = currentTime });
                }
            }
            else if (message.QosLevel == QosLevel.ExactlyOnce) {
                lock (_messagesWaitingForQoS2Pubrec.SyncRoot) {
                    _messagesWaitingForQoS2Pubrec.Add(message.MessageId, new MqttMsgContext() { Message = message, Attempt = 1, Timestamp = currentTime });
                }
            }

            _client.Send(message.GetBytes());
            Trace.WriteLine(TraceLevel.Frame, $"        Publis-> {message.MessageId.ToString("X4")}");

            message.DupFlag = true;
        }

        public void ProcessMessage(MqttMsgPuback message) {
            Trace.WriteLine(TraceLevel.Frame, $"        <-PubAck {message.MessageId.ToString("X4")}");

            lock (_messagesWaitingForQoS1PubAck.SyncRoot) {
                if (_messagesWaitingForQoS1PubAck.Contains(message.MessageId)) {
                    _messagesWaitingForQoS1PubAck.Remove(message.MessageId);
                    NotifyPublishSucceeded(message.MessageId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"        <-Rogue PubAck message for MessageId {message.MessageId.ToString("X4")}");
                    NotifyRogueMessageReceived(message.MessageId);
                }
            }
        }

        public void ProcessMessage(MqttMsgPubrec message) {
            Trace.WriteLine(TraceLevel.Frame, $"        <-PubRec {message.MessageId.ToString("X4")}");
            var currentTime = Environment.TickCount;
            var isOk = true;

            lock (_messagesWaitingForQoS2Pubrec.SyncRoot) {
                if (_messagesWaitingForQoS2Pubrec.Contains(message.MessageId)) {
                    _messagesWaitingForQoS2Pubrec.Remove(message.MessageId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"        <-Rogue Pubrec message for MessageId {message.MessageId.ToString("X4")}");
                    isOk = false;
                    NotifyRogueMessageReceived(message.MessageId);
                }
            }

            if (isOk) {
                var pubrelMessage = new MqttMsgPubrel() { MessageId = message.MessageId };
                var pubrelContext = new MqttMsgContext() { Message = pubrelMessage, Attempt = 1, Timestamp = currentTime };
                lock (_messagesWaitingForQoS2Pubcomp.SyncRoot) {
                    _messagesWaitingForQoS2Pubcomp.Add(message.MessageId, pubrelContext);
                }

                _client.Send((ISentToBroker)pubrelMessage);
                Trace.WriteLine(TraceLevel.Frame, $"        PubRel-> {message.MessageId.ToString("X4")}");
            }
        }

        public void ProcessMessage(MqttMsgPubcomp message) {
            Trace.WriteLine(TraceLevel.Frame, $"        <-PubCom {message.MessageId.ToString("X4")}");

            lock (_messagesWaitingForQoS2Pubcomp.SyncRoot) {
                if (_messagesWaitingForQoS2Pubcomp.Contains(message.MessageId)) {
                    _messagesWaitingForQoS2Pubcomp.Remove(message.MessageId);
                    NotifyPublishSucceeded(message.MessageId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"        <-Rogue Pubcomp message for MessageId {message.MessageId.ToString("X4")}");
                    NotifyRogueMessageReceived(message.MessageId);
                }
            }
        }

        private void ResendAndClean(Hashtable messageTable) {
            var currentTime = Environment.TickCount;

            lock (messageTable.SyncRoot) {
                foreach (DictionaryEntry item in messageTable) {
                    var queuedItem = (MqttMsgContext)item.Value;

                    if (currentTime - queuedItem.Timestamp > MqttSettings.KeepAlivePeriod) {
                        _client.Send((ISentToBroker)queuedItem.Message);
                        queuedItem.Attempt++;
                    }

                    if (queuedItem.Attempt > MqttSettings.AttemptsRetry) {
                        _tempList.Enqueue(item.Key);
                        Trace.WriteLine(TraceLevel.Queuing, $"        Message {queuedItem.Message.MessageId} could no be sent, even after retries.");
                        NotifyPublishFailed(queuedItem.Message.MessageId);
                    }
                }
            }


            while (_tempList.TryDequeue(out var item)) {
                Trace.WriteLine(TraceLevel.Queuing, $"        Cleaning unacknowledged message {item}.");
                lock (messageTable.SyncRoot) {
                    messageTable.Remove(item);
                }
            }
        }

#warning of course, that's not the place to raise events.
        private void NotifyPublishSucceeded(ushort messageId) {
            _client.OnMqttMsgPublished(messageId, true);
        }
        private void NotifyPublishFailed(ushort messageId) {
            _client.OnMqttMsgPublished(messageId, false);
        }
        private void NotifyRogueMessageReceived(ushort messageId) { }

    }
}
