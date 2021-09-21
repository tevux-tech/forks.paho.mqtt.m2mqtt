using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class IncomingPublishStateMachine {
        private MqttClient _client;
        private readonly ResendingStateMachine _qos2PubrecQueue = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _qos2PubrecQueue.Initialize(client);
        }

        public void Tick() {
            _qos2PubrecQueue.Tick();
        }

        public void ProcessPacket(PublishPacket packet) {
            Trace.LogIncomingPacket(packet);

            var currentTime = Helpers.GetCurrentTime();

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending no responses!
                NotifyPublishReceived(packet);
            }
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                var pubAckPacket = new PubackPacket(packet.PacketId);
                _client.Send(pubAckPacket);
                Trace.LogOutgoingPacket(pubAckPacket);
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                var pubRecPacket = new PubrecPacket(packet.PacketId);
                var context = new PublishTransmissionContext(packet, pubRecPacket, currentTime);
                _qos2PubrecQueue.Enqueue(context);
            }
        }

        public void ProcessPacket(PubrelPacket packet) {
            var currentTime = Helpers.GetCurrentTime();
            Trace.LogIncomingPacket(packet);

            var isOk = true;

            var isFinalized = _qos2PubrecQueue.TryFinalize(packet.PacketId, out var finalizedContext);

            if (isFinalized) {
                Trace.LogIncomingPacket(packet);
                NotifyPublishReceived(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                isOk = false;
                NotifyRoguePacketReceived(packet);
            }

            if (isOk) {
                var pubcompPacket = new PubcompPacket(packet.PacketId);
#warning server may ask resend PubRel if for some reason this PubComp is lost. Need to handle that, too.

                _client.Send(pubcompPacket);
                Trace.LogOutgoingPacket(pubcompPacket);
            }
        }

#warning of course, that's not the place to raise events.
        private void NotifyPublishReceived(PublishPacket packet) {
            _client.OnMqttMsgPublishReceived(packet);
        }
        private void NotifyPublishFailed(ushort packetId) {
            // _client.OnMqttMsgPublished(packet, false);
        }
        private void NotifyRoguePacketReceived(ControlPacketBase packet) {
            Trace.LogIncomingPacket(packet, true);
        }

    }
}
