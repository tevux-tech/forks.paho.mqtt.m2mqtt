using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class OutgoingPublishStateMachine {
        private MqttClient _client;
        private readonly ConcurrentQueue _tempList = new ConcurrentQueue();

        private readonly Hashtable _packetsWaitingForQoS1PubAck = new Hashtable();
        private readonly Hashtable _packetsWaitingForQoS2Pubrec = new Hashtable();
        private readonly Hashtable _packetsWaitingForQoS2Pubcomp = new Hashtable();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            ResendAndClean(_packetsWaitingForQoS1PubAck);
            ResendAndClean(_packetsWaitingForQoS2Pubrec);
            ResendAndClean(_packetsWaitingForQoS2Pubcomp);
        }

        public void Publish(PublishPacket packet) {
            var currentTime = Helpers.GetCurrentTime();

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending and waiting for no responses!
            }
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                lock (_packetsWaitingForQoS1PubAck.SyncRoot) {
                    _packetsWaitingForQoS1PubAck.Add(packet.PacketId, new RetransmissionContext() { Packet = packet, Attempt = 1, Timestamp = currentTime });
                }
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                lock (_packetsWaitingForQoS2Pubrec.SyncRoot) {
                    _packetsWaitingForQoS2Pubrec.Add(packet.PacketId, new RetransmissionContext() { Packet = packet, Attempt = 1, Timestamp = currentTime });
                }
            }

            _client.Send(packet.GetBytes());
            Trace.WriteLine(TraceLevel.Frame, $"            Publis-> {packet.PacketId:X4}");

            packet.DupFlag = true;
        }

        public void ProcessPacket(PubackPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"                     {packet.PacketId:X4} <-PubAck");

            lock (_packetsWaitingForQoS1PubAck.SyncRoot) {
                if (_packetsWaitingForQoS1PubAck.Contains(packet.PacketId)) {
                    _packetsWaitingForQoS1PubAck.Remove(packet.PacketId);
                    NotifyPublishSucceeded(packet.PacketId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"            <-Rogue PubAck packet for PacketId {packet.PacketId:X4}");
                    NotifyRoguePacketReceived(packet.PacketId);
                }
            }
        }

        public void ProcessPacket(PubrecPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"                     {packet.PacketId:X4} <-PubRec");
            var currentTime = Helpers.GetCurrentTime();
            var isOk = true;

            lock (_packetsWaitingForQoS2Pubrec.SyncRoot) {
                if (_packetsWaitingForQoS2Pubrec.Contains(packet.PacketId)) {
                    _packetsWaitingForQoS2Pubrec.Remove(packet.PacketId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"            <-Rogue Pubrec packet for PacketId {packet.PacketId:X4}");
                    isOk = false;
                    NotifyRoguePacketReceived(packet.PacketId);
                }
            }

            if (isOk) {
                var pubrelPacket = new PubrelPacket(packet.PacketId);
                var pubrelContext = new RetransmissionContext() { Packet = pubrelPacket, Attempt = 1, Timestamp = currentTime };
                lock (_packetsWaitingForQoS2Pubcomp.SyncRoot) {
                    _packetsWaitingForQoS2Pubcomp.Add(packet.PacketId, pubrelContext);
                }

                _client.Send(pubrelPacket);
                Trace.WriteLine(TraceLevel.Frame, $"            PubRel-> {packet.PacketId:X4}");
            }
        }

        public void ProcessPacket(PubcompPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"                     {packet.PacketId:X4} <-PubCom");

            lock (_packetsWaitingForQoS2Pubcomp.SyncRoot) {
                if (_packetsWaitingForQoS2Pubcomp.Contains(packet.PacketId)) {
                    _packetsWaitingForQoS2Pubcomp.Remove(packet.PacketId);
                    NotifyPublishSucceeded(packet.PacketId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"            <-Rogue Pubcomp packet for PacketId {packet.PacketId:X4}");
                    NotifyRoguePacketReceived(packet.PacketId);
                }
            }
        }

        private void ResendAndClean(Hashtable packetTable) {
            var currentTime = Helpers.GetCurrentTime();

            lock (packetTable.SyncRoot) {
                foreach (DictionaryEntry item in packetTable) {
                    var queuedItem = (RetransmissionContext)item.Value;

                    if (currentTime - queuedItem.Timestamp > _client.ConnectionOptions.RetryDelay) {
                        _client.Send(queuedItem.Packet);
                        queuedItem.Attempt++;
                    }

                    if (queuedItem.Attempt > _client.ConnectionOptions.MaxRetryCount) {
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
        private void NotifyPublishSucceeded(ushort packetId) {
            _client.OnMqttMsgPublished(packetId, true);
        }
        private void NotifyPublishFailed(ushort packetId) {
            _client.OnMqttMsgPublished(packetId, false);
        }
        private void NotifyRoguePacketReceived(ushort packetId) { }

    }
}
