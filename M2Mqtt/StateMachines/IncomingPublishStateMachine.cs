using System;
using System.Collections;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    internal class IncomingPublishStateMachine {
        private MqttClient _client;
        private readonly ConcurrentQueue _tempList = new ConcurrentQueue();

        private readonly Hashtable _messagesQoS1PubAck = new Hashtable();
        private readonly Hashtable _messagesQoS2PubRec = new Hashtable();
        private readonly Hashtable _messagesQoS2PubComp = new Hashtable();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            ResendAndClean(_messagesQoS1PubAck);
            ResendAndClean(_messagesQoS2PubRec);
            ResendAndClean(_messagesQoS2PubComp);
        }

        public void ProcessMessage(MqttMsgPublish message) {
            Trace.WriteLine(TraceLevel.Frame, $"        <-Publis {message.MessageId.ToString("X4")}");

            var currentTime = Environment.TickCount;

            if (message.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best messages - just sending no responses!
                NotifyPublishReceived(message);

            }

#warning Should I check here for duplicate incoming messages? Which may already be in the pipeline?
            else if (message.QosLevel == QosLevel.AtLeastOnce) {
                var pubAckMessage = new MqttMsgPuback() { MessageId = message.MessageId };
                lock (_messagesQoS1PubAck.SyncRoot) {
                    _messagesQoS1PubAck.Add(message.MessageId, new MqttMsgContext() { Message = pubAckMessage, Attempt = 1, Timestamp = currentTime });
                }
                _client.Send(pubAckMessage.GetBytes());
                Trace.WriteLine(TraceLevel.Frame, $"        PubAck-> {pubAckMessage.MessageId.ToString("X4")}");
            }
            else if (message.QosLevel == QosLevel.ExactlyOnce) {
                var pubRecMessage = new MqttMsgPubrec() { MessageId = message.MessageId };
                lock (_messagesQoS2PubRec.SyncRoot) {
                    _messagesQoS2PubRec.Add(message.MessageId, new MqttMsgContext() { Message = pubRecMessage, Attempt = 1, Timestamp = currentTime });
                }
                _client.Send(pubRecMessage.GetBytes());
                Trace.WriteLine(TraceLevel.Frame, $"        PubRec-> {pubRecMessage.MessageId.ToString("X4")}");
            }
        }

        public void ProcessMessage(MqttMsgPubrel message) {
            var currentTime = Environment.TickCount;
            Trace.WriteLine(TraceLevel.Frame, $"        <-PubRel {message.MessageId.ToString("X4")}");

            var isOk = true;

            lock (_messagesQoS2PubRec.SyncRoot) {
                if (_messagesQoS2PubRec.Contains(message.MessageId)) {
                    // var originalMessage = (MqttMsgPublish)_messagesWaitingForQoS2Pubrel[message.MessageId];
                    _messagesQoS2PubRec.Remove(message.MessageId);

#warning need to extract original publish message here
                    // NotifyPublishReceived(message);
                }
                else {
                    isOk = false;
                    Trace.WriteLine(TraceLevel.Queuing, $"        <-Rogue PubRel message for MessageId {message.MessageId.ToString("X4")}");
                    NotifyRogueMessageReceived(message.MessageId);
                }
            }

            if (isOk) {
                var pubcompMessage = new MqttMsgPubcomp() { MessageId = message.MessageId };
#warning server may ask resend PubRel if for some reason this PubComp is lost. Need to handle that, too.

                _client.Send((ISentToBroker)pubcompMessage);
                Trace.WriteLine(TraceLevel.Frame, $"        PubCom-> {message.MessageId.ToString("X4")}");
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
        private void NotifyPublishReceived(MqttMsgPublish message) {
            _client.OnMqttMsgPublishReceived(message);
        }
        private void NotifyPublishFailed(ushort messageId) {
            // _client.OnMqttMsgPublished(messageId, false);
        }
        private void NotifyRogueMessageReceived(ushort messageId) { }

    }
}
