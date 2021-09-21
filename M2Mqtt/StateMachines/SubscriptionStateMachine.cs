using System.Threading;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class SubscriptionStateMachine {
        private readonly string _indent = "                                        ";
        private MqttClient _client;
        private ResendingStateMachine _packetsToSend = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _packetsToSend.Initialize(client);
        }

        public void Tick() {
            _packetsToSend.Tick();
        }

        public bool Subscribe(SubscribePacket packet, bool waitForCompletion = false) {
            return InternalSubscribeUnsubscribe(packet, waitForCompletion);
        }

        public bool Unsubscribe(UnsubscribePacket packet, bool waitForCompletion = false) {
            return InternalSubscribeUnsubscribe(packet, waitForCompletion);
        }

        private bool InternalSubscribeUnsubscribe(ControlPacketBase packet, bool waitForCompletion = false) {
            var currentTime = Helpers.GetCurrentTime();

            var transmissionContext = new TransmissionContext() { PacketToSend = packet, AttemptNumber = 1, Timestamp = currentTime };

            _packetsToSend.Enqueue(transmissionContext);

            if (waitForCompletion) {
                var timeToBreak = false;
                while (timeToBreak == false) {
                    Thread.Sleep(10);
                    if (transmissionContext.IsFinished) { timeToBreak = true; }
                    if (_client.IsConnected == false) { timeToBreak = true; }
                }
            }

            var returnResult = waitForCompletion ? transmissionContext.IsSucceeded : true;

            return returnResult;
        }

        public void ProcessPacket(SubackPacket packet) {
            InternalProcessPacket(packet);
        }
        public void ProcessPacket(UnsubackPacket packet) {
            InternalProcessPacket(packet);
        }

        private void InternalProcessPacket(ControlPacketBase packet) {
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packet.PacketId:X4} <-{packet.GetShortName()}");

            if (_packetsToSend.TryFinalize(packet.PacketId, out var finalizedContext)) {
                _client.OnPacketAcknowledged(finalizedContext.PacketToSend, packet);
            }
            else {
                HandleRoguePacketReceived(packet);
            }
        }

        private void HandleRoguePacketReceived(ControlPacketBase packet) {
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packet.PacketId:X4} <-{packet.GetShortName()} (R)");
        }
    }
}
