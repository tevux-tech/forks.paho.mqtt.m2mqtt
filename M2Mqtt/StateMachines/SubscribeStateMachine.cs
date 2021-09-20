using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class SubscribeStateMachine {
        private MqttClient _client;
        private Hashtable _packetsWaitingForAck = new Hashtable();
        private ConcurrentQueue _tempList = new ConcurrentQueue();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            lock (_packetsWaitingForAck.SyncRoot) {
                foreach (DictionaryEntry item in _packetsWaitingForAck) {
                    var packet = (RetransmissionContext)item.Value;

                    if (currentTime - packet.Timestamp > _client.DelayOnRetry) {
                        _client.Send(packet.Packet);
                        packet.Attempt++;
                    }

                    if (packet.Attempt >= _client.RetryAttemps) {
                        _tempList.Enqueue(item.Key);
                        Trace.WriteLine(TraceLevel.Queuing, $"                                Subscribe packet {packet.Packet.PacketId} could no be sent, even after retries.");
                    }
                }
            }

            var areTherePacketsToRemove = true;
            while (areTherePacketsToRemove) {
                if (_tempList.TryDequeue(out var item)) {
                    Trace.WriteLine(TraceLevel.Queuing, $"                                        Cleaning unacknowledged Subscribe packet {item.ToString()}.");
                    lock (_packetsWaitingForAck.SyncRoot) {
                        _packetsWaitingForAck.Remove(item);
                    }
                }
                else {
                    areTherePacketsToRemove = false;
                }
            }
        }

        public void Subscribe(SubscribePacket packet) {
            var currentTime = Helpers.GetCurrentTime();

            lock (_packetsWaitingForAck.SyncRoot) {
                _packetsWaitingForAck.Add(packet.PacketId, new RetransmissionContext() { Packet = packet, Attempt = 1, Timestamp = currentTime });
            }

            _client.Send(packet);
            Trace.WriteLine(TraceLevel.Frame, $"                                        Subscr-> {packet.PacketId.ToString("X4")}");
        }

        public void ProcessPacket(SubackPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"                                                 {packet.PacketId.ToString("X4")} <-SubAck");

            lock (_packetsWaitingForAck.SyncRoot) {
                if (_packetsWaitingForAck.Contains(packet.PacketId)) {
                    _packetsWaitingForAck.Remove(packet.PacketId);
#warning of course, that's not the place to raise events.
                    _client.OnMqttMsgSubscribed(packet);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"                                Rogue SubAck packet for PacketId {packet.PacketId.ToString("X4")}");
#warning Rogue SubAck message, what do I do now?..
                }
            }
        }
    }
}
