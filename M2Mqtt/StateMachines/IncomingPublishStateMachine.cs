using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class IncomingPublishStateMachine {
        private MqttClient _client;
        private readonly ConcurrentQueue _tempList = new ConcurrentQueue();

        private readonly Hashtable _packetsQoS1PubAck = new Hashtable();
        private readonly Hashtable _packetQoS2PubRec = new Hashtable();
        private readonly Hashtable _packetQoS2PubComp = new Hashtable();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            ResendAndClean(_packetsQoS1PubAck);
            ResendAndClean(_packetQoS2PubRec);
            ResendAndClean(_packetQoS2PubComp);
        }

        public void ProcessPacket(PublishPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"                     {packet.PacketId:X4} <-Publis ");

            var currentTime = Helpers.GetCurrentTime();

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending no responses!
                NotifyPublishReceived(packet);

            }

#warning Should I check here for duplicate incoming messages? Which may already be in the pipeline?
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                var pubAckPacket = new PubackPacket(packet.PacketId);
                lock (_packetsQoS1PubAck.SyncRoot) {
                    _packetsQoS1PubAck.Add(packet.PacketId, new TransmissionContext() { Packet = pubAckPacket, AttemptNumber = 1, Timestamp = currentTime });
                }
                _client.Send(pubAckPacket.GetBytes());
                Trace.WriteLine(TraceLevel.Frame, $"            PubAck-> {pubAckPacket.PacketId:X4}");
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                var pubRecPacket = new PubrecPacket(packet.PacketId);
                lock (_packetQoS2PubRec.SyncRoot) {
                    _packetQoS2PubRec.Add(packet.PacketId, new TransmissionContext() { Packet = pubRecPacket, AttemptNumber = 1, Timestamp = currentTime });
                }
                _client.Send(pubRecPacket.GetBytes());
                Trace.WriteLine(TraceLevel.Frame, $"            PubRec-> {pubRecPacket.PacketId:X4}");
            }
        }

        public void ProcessPacket(PubrelPacket packet) {
            var currentTime = Helpers.GetCurrentTime();
            Trace.WriteLine(TraceLevel.Frame, $"                     {packet.PacketId:X4} <-PubRel");

            var isOk = true;

            lock (_packetQoS2PubRec.SyncRoot) {
                if (_packetQoS2PubRec.Contains(packet.PacketId)) {
                    // var originalMessage = (MqttMsgPublish)_messagesWaitingForQoS2Pubrel[message.MessageId];
                    _packetQoS2PubRec.Remove(packet.PacketId);

#warning need to extract original publish message here
                    // NotifyPublishReceived(message);
                }
                else {
                    isOk = false;
                    Trace.WriteLine(TraceLevel.Queuing, $"            <-Rogue PubRel packet for PacketId {packet.PacketId:X4}");
                    NotifyRoguePacketReceived(packet.PacketId);
                }
            }

            if (isOk) {
                var pubcompPacket = new PubcompPacket(packet.PacketId);
#warning server may ask resend PubRel if for some reason this PubComp is lost. Need to handle that, too.

                _client.Send(pubcompPacket);
                Trace.WriteLine(TraceLevel.Frame, $"            PubCom-> {packet.PacketId:X4}");
            }
        }

        private void ResendAndClean(Hashtable packetTable) {
            var currentTime = Helpers.GetCurrentTime();

            lock (packetTable.SyncRoot) {
                foreach (DictionaryEntry item in packetTable) {
                    var queuedItem = (TransmissionContext)item.Value;

                    if (currentTime - queuedItem.Timestamp > _client.ConnectionOptions.RetryDelay) {
                        _client.Send(queuedItem.Packet);
                        queuedItem.AttemptNumber++;
                    }

                    if (queuedItem.AttemptNumber > _client.ConnectionOptions.MaxRetryCount) {
                        _tempList.Enqueue(item.Key);
                        Trace.WriteLine(TraceLevel.Queuing, $"            Packet {queuedItem.Packet.PacketId} could no be sent, even after retries.");
                        NotifyPublishFailed(queuedItem.Packet.PacketId);
                    }
                }
            }


            while (_tempList.TryDequeue(out var item)) {
                Trace.WriteLine(TraceLevel.Queuing, $"            Cleaning unacknowledged packet {item}.");
                lock (packetTable.SyncRoot) {
                    packetTable.Remove(item);
                }
            }
        }

#warning of course, that's not the place to raise events.
        private void NotifyPublishReceived(PublishPacket packet) {
            _client.OnMqttMsgPublishReceived(packet);
        }
        private void NotifyPublishFailed(ushort packetId) {
            // _client.OnMqttMsgPublished(packet, false);
        }
        private void NotifyRoguePacketReceived(ushort packetId) { }

    }
}
