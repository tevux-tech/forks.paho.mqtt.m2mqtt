using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class UnsubscribeStateMachine {
        private string _traceIndent = "                                        ";
        private ArrayList _unacknowledgedPackets = new ArrayList();
        private double _lastAck;
        private MqttClient _client;

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            if (currentTime - _lastAck > _client.ConnectionOptions.RetryDelay) {
                if (_unacknowledgedPackets.Count > 0) {
                    Trace.WriteLine(TraceLevel.Queuing, $"Cleaning unacknowledged Unsubscribe packet.");
#warning Server did not acknowledged all unsubscribe messages. Is this a protocol violation?..
                    lock (_unacknowledgedPackets.SyncRoot) {
                        _unacknowledgedPackets.Clear();
                    }
                }
            }
        }

        public void Unsubscribe(UnsubscribePacket packet) {
            lock (_unacknowledgedPackets.SyncRoot) {
                _unacknowledgedPackets.Add(packet);
            }

            _client.Send(packet);
            Trace.WriteLine(TraceLevel.Frame, $"{_traceIndent}UnsubA-> {packet.PacketId:X4}");
        }

        public void ProcessPacket(UnsubackPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"{_traceIndent}         {packet.PacketId:X4} <-Unsubs");

            _lastAck = Helpers.GetCurrentTime();

            lock (_unacknowledgedPackets.SyncRoot) {
                UnsubscribePacket foundPacket = null;
                foreach (UnsubscribePacket unsubscribePacket in _unacknowledgedPackets) {
                    if (unsubscribePacket.PacketId == packet.PacketId) {
                        foundPacket = unsubscribePacket;
                        break;
                    }
                }

                if (foundPacket != null) {
                    _unacknowledgedPackets.Remove(foundPacket);
#warning of course, that's not the place to raise events.
                    _client.OnMqttMsgUnsubscribed(packet.PacketId);
                }
                else {
                    Trace.WriteLine(TraceLevel.Queuing, $"{_traceIndent}Rogue UnsubAck packet for PacketId {packet.PacketId:X4}");
#warning Rogue UnsubAck message?..
                }
            }
        }
    }
}
