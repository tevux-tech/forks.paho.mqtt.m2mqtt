using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class IncomingPublishStateMachine {
        private string _indent = "            ";
        private MqttClient _client;

        private ResendingStateMachine _qos1PubackQueue = new ResendingStateMachine();
        private ResendingStateMachine _qos2PubrecQueue = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _qos1PubackQueue.Initialize(client);
            _qos2PubrecQueue.Initialize(client);
        }

        public void Tick() {
            _qos1PubackQueue.Tick();
            _qos2PubrecQueue.Tick();
        }

        public void ProcessPacket(PublishPacket packet) {
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packet.PacketId:X4} <-{packet.GetShortName()} ");

            var currentTime = Helpers.GetCurrentTime();

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending no responses!
                NotifyPublishReceived(packet);

            }

#warning Should I check here for duplicate incoming messages? Which may already be in the pipeline?
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                var pubAckPacket = new PubackPacket(packet.PacketId);
                var context = new PublishTransmissionContext() { OriginalPublishPacket = packet, PacketToSend = pubAckPacket, AttemptNumber = 1, Timestamp = currentTime };
                _qos1PubackQueue.Enqueue(context);
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                var pubRecPacket = new PubrecPacket(packet.PacketId);
                var context = new PublishTransmissionContext() { OriginalPublishPacket = packet, PacketToSend = pubRecPacket, AttemptNumber = 1, Timestamp = currentTime };
                _qos2PubrecQueue.Enqueue(context);
            }
        }

        public void ProcessPacket(PubrelPacket packet) {
            var currentTime = Helpers.GetCurrentTime();
            Trace.WriteLine(TraceLevel.Frame, $"{_indent}         {packet.PacketId:X4} <-{packet.GetShortName()}");

            var isOk = true;

            var isFinalized = _qos2PubrecQueue.TryFinalize(packet.PacketId, out var finalizedContext);

            if (isFinalized) {
                NotifyPublishReceived(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                isOk = false;
                Trace.WriteLine(TraceLevel.Queuing, $"{_indent}<-Rogue {packet.GetShortName()} packet for PacketId {packet.PacketId:X4}");
                NotifyRoguePacketReceived(packet.PacketId);
            }

            if (isOk) {
                var pubcompPacket = new PubcompPacket(packet.PacketId);
#warning server may ask resend PubRel if for some reason this PubComp is lost. Need to handle that, too.

                _client.Send(pubcompPacket);
                Trace.WriteLine(TraceLevel.Frame, $"{_indent}{pubcompPacket.GetShortName()}-> {pubcompPacket.PacketId:X4}");
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
