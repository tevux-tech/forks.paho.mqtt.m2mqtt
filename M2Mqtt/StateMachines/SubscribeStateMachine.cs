using System.Collections;
using System.Threading;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class SubscribeStateMachine {
        private readonly string _indent = "                                        ";
        private MqttClient _client;
        private readonly Hashtable _packetsWaitingForAck = new Hashtable();
        private readonly ConcurrentQueue _tempList = new ConcurrentQueue();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            lock (_packetsWaitingForAck.SyncRoot) {
                foreach (DictionaryEntry item in _packetsWaitingForAck) {
                    var context = (TransmissionContext)item.Value;

                    if (context.AttemptNumber >= _client.ConnectionOptions.MaxRetryCount) {
                        context.IsFinished = true;
                        context.IsSucceeded = false;
                        _tempList.Enqueue(context);
                    }
                    else if (currentTime - context.Timestamp > _client.ConnectionOptions.RetryDelay) {
                        context.AttemptNumber++;
                        context.Timestamp = currentTime;
                        Trace.WriteLine(TraceLevel.Frame, $"{_indent}Subscr-> {context.PacketId:X4} ({context.AttemptNumber})");
                        _client.Send(context.Packet);
                    }
                }
            }


            while (_tempList.TryDequeue(out var item)) {
                var context = (TransmissionContext)item;
                Trace.WriteLine(TraceLevel.Queuing, $"{_indent}         {context.Packet.PacketId:X4} FAILED");
                lock (_packetsWaitingForAck.SyncRoot) {
                    _packetsWaitingForAck.Remove(context.PacketId);
                }
            }
        }

        public bool Subscribe(SubscribePacket packet, bool waitForCompletion = false) {
            var currentTime = Helpers.GetCurrentTime();

            var transmissionContext = new TransmissionContext() { Packet = packet, AttemptNumber = 1, Timestamp = currentTime };

            lock (_packetsWaitingForAck.SyncRoot) {
                _packetsWaitingForAck.Add(transmissionContext.PacketId, transmissionContext);
            }

            _client.Send(packet);
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}Subscr-> {packet.PacketId:X4}");

            if (waitForCompletion) {
                var timeToBreak = false;
                while (timeToBreak == false) {
                    Thread.Sleep(10);
                    if (transmissionContext.IsFinished) { timeToBreak = true; }
                    if (_client.IsConnected == false) { timeToBreak = true; }
                }
            }

            return transmissionContext.IsSucceeded;
        }

        public void ProcessPacket(SubackPacket packet) {
            //if ((new Random()).Next(2) == 1) { return; }
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packet.PacketId:X4} <-SubAck");

            lock (_packetsWaitingForAck.SyncRoot) {
                if (_packetsWaitingForAck.Contains(packet.PacketId)) {
                    var contextToRemove = (TransmissionContext)_packetsWaitingForAck[packet.PacketId];
                    contextToRemove.IsFinished = true;
                    contextToRemove.IsSucceeded = true;
                    _packetsWaitingForAck.Remove(packet.PacketId);
                    NotifySubscribed(((SubscribePacket)contextToRemove.Packet).Topic, packet.GrantedQosLevel);
                }
                else {
                    HandleRoguePacketReceived(packet.PacketId, packet.Type);
                }
            }
        }

        private void HandleRoguePacketReceived(ushort packetId, byte type) {
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packetId:X4} <-SubAck (R)");
        }
        private void NotifySubscribed(string topic, GrantedQosLevel grantedQosLevel) {
            _client.OnMqttMsgSubscribed(topic, grantedQosLevel);
        }
    }
}
